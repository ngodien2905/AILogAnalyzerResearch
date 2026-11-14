CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE coding_tasks (
        id uuid NOT NULL,
        task_code character varying(24) NOT NULL,
        title character varying(160) NOT NULL,
        description character varying(1200),
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_coding_tasks_id PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE participants (
        id uuid NOT NULL,
        participant_code character varying(16) NOT NULL,
        course character varying(8) NOT NULL,
        research_group character varying(40),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_participants_id PRIMARY KEY (id),
        CONSTRAINT ck_participant_code_not_blank CHECK (participant_code <> '')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE research_outcomes (
        id uuid NOT NULL,
        participant_id uuid NOT NULL,
        metric_name character varying(60) NOT NULL,
        value numeric(10,3) NOT NULL,
        scale_maximum numeric(10,3),
        recorded_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_research_outcomes_id PRIMARY KEY (id),
        CONSTRAINT ck_research_outcome_scale CHECK (scale_maximum IS NULL OR scale_maximum > 0),
        CONSTRAINT fk_research_outcomes_participants_participant_id FOREIGN KEY (participant_id) REFERENCES participants (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE task_assignments (
        id uuid NOT NULL,
        participant_id uuid NOT NULL,
        coding_task_id uuid NOT NULL,
        assigned_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_task_assignments_id PRIMARY KEY (id),
        CONSTRAINT fk_task_assignments_coding_tasks_coding_task_id FOREIGN KEY (coding_task_id) REFERENCES coding_tasks (id) ON DELETE CASCADE,
        CONSTRAINT fk_task_assignments_participants_participant_id FOREIGN KEY (participant_id) REFERENCES participants (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE ai_interactions (
        id uuid NOT NULL,
        task_assignment_id uuid NOT NULL,
        platform character varying(60) NOT NULL,
        plan character varying(16) NOT NULL,
        prompt text NOT NULL,
        response text NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        imported_at timestamp with time zone NOT NULL,
        source character varying(32) NOT NULL,
        is_valid boolean NOT NULL,
        validation_note character varying(400),
        CONSTRAINT pk_ai_interactions_id PRIMARY KEY (id),
        CONSTRAINT fk_ai_interactions_task_assignments_task_assignment_id FOREIGN KEY (task_assignment_id) REFERENCES task_assignments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE source_submissions (
        id uuid NOT NULL,
        task_assignment_id uuid NOT NULL,
        original_file_name character varying(255) NOT NULL,
        storage_reference character varying(600) NOT NULL,
        content_type character varying(120) NOT NULL,
        size_bytes bigint NOT NULL,
        submitted_at timestamp with time zone NOT NULL,
        is_final boolean NOT NULL,
        CONSTRAINT pk_source_submissions_id PRIMARY KEY (id),
        CONSTRAINT fk_source_submissions_task_assignments_task_assignment_id FOREIGN KEY (task_assignment_id) REFERENCES task_assignments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE ai_classifications (
        id uuid NOT NULL,
        ai_interaction_id uuid NOT NULL,
        category character varying(40) NOT NULL,
        review_status character varying(24) NOT NULL,
        reviewed_at timestamp with time zone,
        reviewer_note character varying(400),
        CONSTRAINT pk_ai_classifications_id PRIMARY KEY (id),
        CONSTRAINT fk_ai_classifications_ai_interactions_ai_interaction_id FOREIGN KEY (ai_interaction_id) REFERENCES ai_interactions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE TABLE coding_assessments (
        id uuid NOT NULL,
        source_submission_id uuid NOT NULL,
        correctness numeric(5,2) NOT NULL,
        code_quality numeric(5,2) NOT NULL,
        solution_efficiency numeric(5,2) NOT NULL,
        testing_error_handling numeric(5,2) NOT NULL,
        ability_to_explain numeric(5,2) NOT NULL,
        notes character varying(1000),
        assessed_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_coding_assessments_id PRIMARY KEY (id),
        CONSTRAINT ck_assessment_code_quality CHECK (code_quality >= 0 AND code_quality <= 2),
        CONSTRAINT ck_assessment_correctness CHECK (correctness >= 0 AND correctness <= 4),
        CONSTRAINT ck_assessment_explanation CHECK (ability_to_explain >= 0 AND ability_to_explain <= 2),
        CONSTRAINT ck_assessment_solution_efficiency CHECK (solution_efficiency >= 0 AND solution_efficiency <= 1),
        CONSTRAINT ck_assessment_testing CHECK (testing_error_handling >= 0 AND testing_error_handling <= 1),
        CONSTRAINT fk_coding_assessments_source_submissions_source_submission_id FOREIGN KEY (source_submission_id) REFERENCES source_submissions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_ai_classifications_ai_interaction_id ON ai_classifications (ai_interaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE INDEX ix_ai_interactions_task_assignment_id ON ai_interactions (task_assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_coding_assessments_source_submission_id ON coding_assessments (source_submission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_coding_tasks_task_code ON coding_tasks (task_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_participants_participant_code ON participants (participant_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_research_outcomes_participant_id_metric_name ON research_outcomes (participant_id, metric_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE INDEX ix_source_submissions_task_assignment_id ON source_submissions (task_assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE INDEX ix_task_assignments_coding_task_id ON task_assignments (coding_task_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    CREATE UNIQUE INDEX ix_task_assignments_participant_id_coding_task_id ON task_assignments (participant_id, coding_task_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111032253_InitialResearchSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111032253_InitialResearchSchema', '8.0.11');
    END IF;
END $EF$;
COMMIT;
