USE [HomeAffairsDb];
GO

CREATE OR ALTER PROCEDURE
    [applications].Application_Search
    @ApplicationNumber NVARCHAR(30) = NULL,
    @ApplicationType NVARCHAR(60) = NULL,
    @Status NVARCHAR(30) = NULL,
    @ApplicantCitizenId UNIQUEIDENTIFIER = NULL,
    @SubjectCitizenId UNIQUEIDENTIFIER = NULL,
    @AssignedToUserId UNIQUEIDENTIFIER = NULL,
    @CreatedFromUtc DATETIME2(7) = NULL,
    @CreatedToUtc DATETIME2(7) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
    BEGIN;
        THROW 52801,
            'Page number must be greater than zero.',
            1;
    END;

    IF @PageSize < 1
       OR @PageSize > 100
    BEGIN;
        THROW 52802,
            'Page size must be between 1 and 100.',
            1;
    END;

    IF @CreatedFromUtc IS NOT NULL
       AND @CreatedToUtc IS NOT NULL
       AND @CreatedFromUtc > @CreatedToUtc
    BEGIN;
        THROW 52803,
            'Created-from date may not be later than created-to date.',
            1;
    END;

    DECLARE @Offset INT =
        (@PageNumber - 1) * @PageSize;

    /*
        Resolve matching application identifiers first so
        the count and page use exactly the same predicates.
    */
    SELECT
        a.ApplicationId
    INTO #MatchingApplications
    FROM [applications].Applications AS a
    WHERE
        (
            @ApplicationNumber IS NULL
            OR a.ApplicationNumber LIKE
                N'%' + @ApplicationNumber + N'%'
        )
        AND
        (
            @ApplicationType IS NULL
            OR a.ApplicationType =
                @ApplicationType
        )
        AND
        (
            @Status IS NULL
            OR a.Status =
                @Status
        )
        AND
        (
            @ApplicantCitizenId IS NULL
            OR a.ApplicantCitizenId =
                @ApplicantCitizenId
        )
        AND
        (
            @SubjectCitizenId IS NULL
            OR a.SubjectCitizenId =
                @SubjectCitizenId
        )
        AND
        (
            @AssignedToUserId IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM [applications].ApplicationAssignments
                    AS assignment
                WHERE assignment.ApplicationId =
                        a.ApplicationId
                  AND assignment.AssignedToUserId =
                        @AssignedToUserId
                  AND assignment.IsActive = 1
            )
        )
        AND
        (
            @CreatedFromUtc IS NULL
            OR a.CreatedAtUtc >=
                @CreatedFromUtc
        )
        AND
        (
            @CreatedToUtc IS NULL
            OR a.CreatedAtUtc <=
                @CreatedToUtc
        );

    /*
        Result set 1:
        Total number of matching records before paging.
    */
    SELECT
        COUNT(1) AS TotalCount
    FROM #MatchingApplications;

    /*
        Result set 2:
        Requested page.
    */
    SELECT
        a.ApplicationId,
        a.ApplicationNumber,
        a.ApplicationType,
        a.ApplicantCitizenId,
        a.SubjectCitizenId,
        a.Status,
        a.SubmittedAtUtc,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [applications].Applications AS a
    INNER JOIN #MatchingApplications AS matching
        ON matching.ApplicationId =
            a.ApplicationId
    ORDER BY
        a.CreatedAtUtc DESC,
        a.ApplicationId DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE #MatchingApplications;
END;
GO
