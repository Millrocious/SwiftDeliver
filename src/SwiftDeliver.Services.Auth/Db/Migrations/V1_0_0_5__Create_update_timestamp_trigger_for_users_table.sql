CREATE TRIGGER dbo.TR_Users_UpdateTimestamp ON dbo.Users AFTER UPDATE AS
    IF (ROWCOUNT_BIG() = 0)
        RETURN;
    BEGIN
        UPDATE u
        SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Users u
        INNER JOIN inserted i
        ON u.Id = i.Id
    end
