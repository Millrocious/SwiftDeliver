INSERT INTO Users(Email, PasswordHash, PasswordSalt, RoleId) 
SELECT 
    'admin@admin.com', 
    'vaXgOLmvksyQMcGqA4B+E0g7AnDcMbXgWaJhldUp4yk=',
    'eRdR4YuRgg0ON9+jXFlURA==',
    Id
FROM
    Roles r
WHERE
    r.Name = 'Admin' 

