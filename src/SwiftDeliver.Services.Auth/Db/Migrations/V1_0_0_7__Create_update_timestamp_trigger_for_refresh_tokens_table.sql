CREATE TRIGGER dbo.TR_RefreshTokens_UpdateUpdatedAtColumn ON dbo.RefreshTokens AFTER UPDATE AS
    IF (ROWCOUNT_BIG() = 0)
        RETURN;
    BEGIN
        UPDATE rt
        SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.RefreshTokens rt
        INNER JOIN inserted i
        ON rt.Id = i.Id
    end
