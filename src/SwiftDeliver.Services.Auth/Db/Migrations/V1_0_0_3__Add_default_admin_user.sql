INSERT INTO Users(Email, PasswordHash, RoleId, CreatedAt) 
SELECT 
    'admin@admin.com', 
    '$2a$12$fSsvHspeVZl7XckMQfDm/usQ8dkld2bS3rrVAk8gASG.GtXQT0lcy',
    Id,
    GETDATE()
FROM
    Roles r
WHERE
    r.Name = 'Admin' 

