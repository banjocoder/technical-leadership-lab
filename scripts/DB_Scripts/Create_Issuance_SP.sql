USE [PolicyDb]
GO

/****** Object:  StoredProcedure [dbo].[IssuePolicy]    Script Date: 10/26/2026 12:35:56 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE   PROCEDURE [dbo].[IssuePolicy]
    @PolicyHolderName nvarchar(100),
    @LineOfBusiness   nvarchar(50),
    @RequestID        uniqueidentifier,
    @EffectiveDate    date
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsNew bit = 0;

    -- These checks also protect callers that bypass the API.
    IF @RequestID IS NULL
       OR @RequestID = '00000000-0000-0000-0000-000000000000'
    BEGIN
        THROW 50001, 'A non-empty RequestID is required.', 1;
    END;

    IF NULLIF(LTRIM(RTRIM(@PolicyHolderName)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@LineOfBusiness)), N'') IS NULL
       OR @EffectiveDate IS NULL
    BEGIN
        THROW 50002, 'Policyholder, line of business, and effective date are required.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Policy
        WHERE RequestID = @RequestID
    )
    BEGIN
        BEGIN TRY
            DECLARE @SequenceNumber bigint =
                NEXT VALUE FOR dbo.PolicyNumberSequence;

            DECLARE @PolicyNumber nvarchar(12) =
                N'POL' + RIGHT(
                    N'000000000'
                    + CONVERT(nvarchar(9), @SequenceNumber),
                    9
                );

            -- One INSERT saves the policy and request identity together.
            INSERT INTO dbo.Policy
            (
                PolicyNumber,
                TermNumber,
                Lob,
                EffectiveDate,
                ExpirationDate,
                PolicyHolderName,
                RequestID,
                CreatedAt
            )
            VALUES
            (
                @PolicyNumber,
                1,
                @LineOfBusiness,
                @EffectiveDate,
                DATEADD(year, 1, @EffectiveDate),
                @PolicyHolderName,
                @RequestID,
                SYSUTCDATETIME()
            );

            SET @IsNew = 1;
        END TRY
        BEGIN CATCH
            -- A concurrent retry may have inserted this request first.
            -- Only recover when a duplicate error has a matching RequestID.
            IF ERROR_NUMBER() NOT IN (2601, 2627)
            BEGIN
                THROW;
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.Policy
                WHERE RequestID = @RequestID
            )
            BEGIN
                THROW;
            END;
        END CATCH;
    END;

    -- Reusing a RequestID must not change its original meaning.
    -- String comparisons here follow the database column collations.
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Policy
        WHERE RequestID = @RequestID
          AND
          (
              PolicyHolderName <> @PolicyHolderName
              OR Lob <> @LineOfBusiness
              OR EffectiveDate <> @EffectiveDate
          )
    )
    BEGIN
        THROW 50003, 'RequestID already exists with different issuance inputs.', 1;
    END;

    SELECT
        PolicyID,
        PolicyHolderName,
        Lob,
        CreatedAt,
        PolicyNumber,
        TermNumber,
        EffectiveDate,
        ExpirationDate,
        @IsNew AS IsNew
    FROM dbo.Policy
    WHERE RequestID = @RequestID;
END;
GO


