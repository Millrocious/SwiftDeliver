CREATE TRIGGER dbo.TR_Roles_UpdateUpdatedAtColumn ON dbo.Roles AFTER UPDATE AS
    IF (ROWCOUNT_BIG() = 0)
        RETURN;
    BEGIN
        UPDATE r
        SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Roles r
        INNER JOIN inserted i
        ON r.Id = i.Id
    end
