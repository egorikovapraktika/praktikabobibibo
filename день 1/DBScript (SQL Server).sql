-- ============================================================
-- Полностью исправленный скрипт для SQL Server
-- ============================================================

-- Удаление существующих объектов (для чистой переустановки)
BEGIN TRY
    DECLARE @sql NVARCHAR(MAX) = ''
    SELECT @sql = @sql + 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.foreign_keys
    SELECT @sql = @sql + 'DROP TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + QUOTENAME(OBJECT_NAME(object_id)) + ';' + CHAR(13)
    FROM sys.tables
    EXEC sp_executesql @sql
END TRY
BEGIN CATCH END CATCH
GO

-- ============================================================
-- Таблицы
-- ============================================================
CREATE TABLE roles (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE departments (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(150) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id),
    email NVARCHAR(100),
    phone NVARCHAR(30),
    is_active BIT NOT NULL DEFAULT 1,
    last_login DATETIME2(3),
    created_at DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    department_id INT REFERENCES departments(id)
);

CREATE TABLE products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    type NVARCHAR(50),
    form NVARCHAR(50),
    status NVARCHAR(20) NOT NULL DEFAULT 'draft'
        CHECK (status IN ('draft','active','archived'))
);

CREATE TABLE raw_materials (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    unit NVARCHAR(20) NOT NULL,
    category NVARCHAR(50)
);

CREATE TABLE recipes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL REFERENCES products(id),
    version INT NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'draft'
        CHECK (status IN ('draft','active','archived')),
    created_at DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    approved_at DATETIME2(3),
    created_by INT REFERENCES users(id),
    UNIQUE(product_id, version)
);

CREATE TABLE recipe_components (
    id INT IDENTITY(1,1) PRIMARY KEY,
    recipe_id INT NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    raw_material_id INT NOT NULL REFERENCES raw_materials(id),
    percentage DECIMAL(5,2) NOT NULL
        CHECK (percentage > 0 AND percentage <= 100),
    load_order INT NOT NULL DEFAULT 0,
    tolerance_min DECIMAL(5,2) DEFAULT 0,
    tolerance_max DECIMAL(5,2) DEFAULT 0
);

CREATE TABLE tech_maps (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL REFERENCES products(id),
    version INT NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'draft'
        CHECK (status IN ('draft','active','archived')),
    created_at DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    created_by INT REFERENCES users(id),
    UNIQUE(product_id, version)
);

CREATE TABLE tech_map_steps (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tech_map_id INT NOT NULL REFERENCES tech_maps(id) ON DELETE CASCADE,
    step_order INT NOT NULL,
    step_name NVARCHAR(100) NOT NULL,
    step_type NVARCHAR(50) NOT NULL,
    planned_temp_c DECIMAL(5,1),
    planned_pressure_bar DECIMAL(4,1),
    planned_duration_min INT,
    is_mandatory BIT NOT NULL DEFAULT 1,
    instruction NVARCHAR(MAX)
);

CREATE TABLE production_orders (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_number NVARCHAR(20) NOT NULL UNIQUE,
    recipe_id INT NOT NULL REFERENCES recipes(id),
    planned_quantity_kg DECIMAL(10,2) NOT NULL
        CHECK (planned_quantity_kg > 0),
    status NVARCHAR(20) NOT NULL DEFAULT 'draft'
        CHECK (status IN ('draft','planned','in_progress','completed','archived')),
    planned_start_date DATE,
    created_by INT REFERENCES users(id)
);

CREATE TABLE batches (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_number NVARCHAR(20) NOT NULL UNIQUE,
    order_id INT NOT NULL REFERENCES production_orders(id),
    recipe_id INT NOT NULL REFERENCES recipes(id),
    tech_map_id INT NOT NULL REFERENCES tech_maps(id),
    start_time DATETIME2(3),
    end_time DATETIME2(3),
    status NVARCHAR(20) NOT NULL DEFAULT 'planned'
        CHECK (status IN ('planned','running','completed','aborted')),
    actual_quantity_kg DECIMAL(10,2)
        CHECK (actual_quantity_kg >= 0)
);

CREATE TABLE batch_steps (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
    step_order INT NOT NULL,
    step_name NVARCHAR(100) NOT NULL,
    planned_temp_c DECIMAL(5,1),
    actual_temp_c DECIMAL(5,1),
    planned_duration_min INT,
    actual_duration_min INT,
    planned_pressure_bar DECIMAL(4,1),
    actual_pressure_bar DECIMAL(4,1),
    started_by INT REFERENCES users(id),
    completed_by INT REFERENCES users(id),
    started_at DATETIME2(3),
    completed_at DATETIME2(3),
    deviation_flag BIT NOT NULL DEFAULT 0,
    operator_comment NVARCHAR(MAX)
);

CREATE TABLE raw_material_batches (
    id INT IDENTITY(1,1) PRIMARY KEY,
    raw_material_id INT NOT NULL REFERENCES raw_materials(id),
    batch_number NVARCHAR(50) NOT NULL UNIQUE,
    supplier NVARCHAR(100),
    received_date DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    quantity DECIMAL(10,2) NOT NULL CHECK (quantity > 0),
    unit NVARCHAR(20) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending','in_analysis','approved','blocked'))
);

CREATE TABLE quality_controls (
    id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT REFERENCES batches(id),
    raw_material_batch_id INT REFERENCES raw_material_batches(id),
    analysis_date DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    sample_type NVARCHAR(50) NOT NULL,
    parameter_name NVARCHAR(100) NOT NULL,
    measured_value DECIMAL(8,2),
    standard_value NVARCHAR(50),
    unit NVARCHAR(20),
    result NVARCHAR(20) CHECK (result IN ('pass','fail')),
    decision NVARCHAR(20) CHECK (decision IN ('approved','blocked')),
    analyst_id INT REFERENCES users(id),
    analyst_comment NVARCHAR(MAX)
);

CREATE TABLE audit_log (
    id INT IDENTITY(1,1) PRIMARY KEY,
    table_name NVARCHAR(50) NOT NULL,
    record_id INT NOT NULL,
    action NVARCHAR(20) NOT NULL,    -- INSERT, UPDATE, DELETE, STATUS_CHANGE
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    changed_by INT REFERENCES users(id),
    changed_at DATETIME2(3) NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- ТРИГГЕРЫ
-- ============================================================

-- Правило 1: Только одна активная рецептура на продукт
CREATE OR ALTER TRIGGER trg_recipe_single_active
ON recipes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT product_id
        FROM (SELECT product_id, status FROM inserted UNION ALL SELECT product_id, status FROM deleted) AS allrows
        WHERE status = 'active'
        GROUP BY product_id
        HAVING COUNT(*) > 1
    )
    BEGIN
        RAISERROR(N'Для продукта уже существует действующая рецептура.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- Правило 2: Только одна активная технологическая карта на продукт
CREATE OR ALTER TRIGGER trg_techmap_single_active
ON tech_maps
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT product_id
        FROM (SELECT product_id, status FROM inserted UNION ALL SELECT product_id, status FROM deleted) AS allrows
        WHERE status = 'active'
        GROUP BY product_id
        HAVING COUNT(*) > 1
    )
    BEGIN
        RAISERROR(N'Для продукта уже существует действующая технологическая карта.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- Правило 3: Сумма компонентов рецептуры должна быть 100% перед активацией
CREATE OR ALTER TRIGGER trg_recipe_activation
ON recipes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(status)
    BEGIN
        DECLARE @id INT, @total DECIMAL(5,2), @msg NVARCHAR(200);
        DECLARE cur CURSOR LOCAL FOR
            SELECT i.id
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE i.status = 'active'
              AND (d.status != 'active' OR d.status IS NULL);
        OPEN cur;
        FETCH NEXT FROM cur INTO @id;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @total = COALESCE(SUM(percentage), 0)
            FROM recipe_components
            WHERE recipe_id = @id;

            IF @total != 100.00
            BEGIN
                SET @msg = CAST(@total AS NVARCHAR(20));
                RAISERROR(N'Сумма долей компонентов не равна 100%% (текущая сумма = %s)', 16, 1, @msg);
                ROLLBACK TRANSACTION;
                RETURN;
            END
            FETCH NEXT FROM cur INTO @id;
        END
        CLOSE cur;
        DEALLOCATE cur;
    END
END;
GO

-- ============================================================
-- НАЧАЛЬНЫЕ ДАННЫЕ (все строки с префиксом N)
-- ============================================================

INSERT INTO roles (name) VALUES
(N'technologist'),
(N'operator'),
(N'laboratory'),
(N'admin'),
(N'engineer'),
(N'manager'),
(N'analyst'),
(N'observer'),
(N'shift_supervisor');

INSERT INTO departments (name) VALUES
(N'Технологический отдел'),
(N'Цех №1'),
(N'Цех №2'),
(N'Цех №3'),
(N'Лаборатория контроля качества'),
(N'Лаборатория сырья'),
(N'IT-отдел'),
(N'Инженерный отдел'),
(N'Управление производством'),
(N'Аналитический отдел'),
(N'Отдел качества'),
(N'Производство');

INSERT INTO users (username, password_hash, full_name, role_id, email, phone, is_active, last_login, created_at, department_id)
SELECT
    u.username,
    u.password_hash,
    u.full_name,
    r.id,
    u.email,
    u.phone,
    u.is_active,
    u.last_login,
    u.created_at,
    d.id
FROM (
    VALUES
    (N'tech.ivanov',   N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Иванов Иван Петрович',         N'technologist', N'ivan.ivanov@agrocontrol.ru',         N'+7 (495) 123-45-01', 1, '2025-03-10 08:30:00', '2024-01-15 10:00:00', N'Технологический отдел'),
    (N'tech.petrova',  N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Петрова Мария Сергеевна',      N'technologist', N'maria.petrova@agrocontrol.ru',      N'+7 (495) 123-45-02', 1, '2025-03-09 16:45:00', '2024-02-20 11:30:00', N'Технологический отдел'),
    (N'tech.smirnov',  N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Смирнов Алексей Викторович',  N'technologist', N'alexey.smirnov@agrocontrol.ru',      N'+7 (495) 123-45-03', 1, '2025-03-08 11:20:00', '2024-03-10 09:15:00', N'Технологический отдел'),
    (N'tech.kuznetsov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Кузнецов Дмитрий Андреевич',  N'technologist', N'dmitry.kuznetsov@agrocontrol.ru',    N'+7 (495) 123-45-04', 0, '2025-02-20 14:10:00', '2024-04-05 13:00:00', N'Технологический отдел'),
    (N'operator.zavodov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Заводов Сергей Николаевич',   N'operator',     N'sergey.zavodov@agrocontrol.ru',    N'+7 (495) 123-45-11', 1, '2025-03-10 07:50:00', '2024-05-20 08:00:00', N'Цех №1'),
    (N'operator.melnikov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Мельников Андрей Геннадьевич',N'operator',     N'andrey.melnikov@agrocontrol.ru',   N'+7 (495) 123-45-12', 1, '2025-03-09 07:30:00', '2024-06-10 09:00:00', N'Цех №1'),
    (N'operator.gromov', N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Громов Илья Дмитриевич',       N'operator',     N'ilya.gromov@agrocontrol.ru',       N'+7 (495) 123-45-13', 1, '2025-03-08 22:15:00', '2024-07-15 10:00:00', N'Цех №2'),
    (N'operator.volkov', N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Волков Роман Александрович',   N'operator',     N'roman.volkov@agrocontrol.ru',      N'+7 (495) 123-45-14', 1, '2025-03-09 23:40:00', '2024-08-01 11:45:00', N'Цех №2'),
    (N'operator.sokolov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Соколов Павел Олегович',        N'operator',     N'pavel.sokolov@agrocontrol.ru',     N'+7 (495) 123-45-15', 0, '2025-02-25 08:00:00', '2024-09-10 13:30:00', N'Цех №3'),
    (N'lab.vasilieva',  N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Васильева Елена Андреевна',    N'laboratory',   N'elena.vasilieva@agrocontrol.ru',   N'+7 (495) 123-45-21', 1, '2025-03-10 09:15:00', '2024-01-20 10:00:00', N'Лаборатория контроля качества'),
    (N'lab.morozova',   N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Морозова Татьяна Игоревна',     N'laboratory',   N'tatiana.morozova@agrocontrol.ru',  N'+7 (495) 123-45-22', 1, '2025-03-09 14:30:00', '2024-03-15 09:30:00', N'Лаборатория контроля качества'),
    (N'lab.nikitina',   N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Никитина Ольга Владимировна',  N'laboratory',   N'olga.nikitina@agrocontrol.ru',     N'+7 (495) 123-45-23', 1, '2025-03-08 11:00:00', '2024-05-10 14:00:00', N'Лаборатория сырья'),
    (N'admin.sidorov',  N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', N'Сидоров Константин Петрович',  N'admin',        N'admin@agrocontrol.ru',             N'+7 (495) 123-45-00', 1, '2025-03-10 08:00:00', '2024-01-01 09:00:00', N'IT-отдел'),
    (N'engineer.mikhailov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Михайлов Артём Витальевич', N'engineer',    N'artem.mikhailov@agrocontrol.ru',   N'+7 (495) 123-45-31', 1, '2025-03-09 17:20:00', '2024-02-10 12:00:00', N'Инженерный отдел'),
    (N'engineer.belov', N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Белов Денис Сергеевич',        N'engineer',     N'denis.belov@agrocontrol.ru',       N'+7 (495) 123-45-32', 0, '2025-02-28 15:40:00', '2024-06-20 11:00:00', N'Инженерный отдел'),
    (N'shift.titov',    N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Титов Максим Ильич',           N'shift_supervisor',N'maxim.titov@agrocontrol.ru',      N'+7 (495) 123-45-41', 1, '2025-03-10 06:45:00', '2024-03-25 08:30:00', N'Производство'),
    (N'shift.frolov',   N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Фролов Николай Алексеевич',     N'shift_supervisor',N'nikolay.frolov@agrocontrol.ru',   N'+7 (495) 123-45-42', 1, '2025-03-09 18:30:00', '2024-07-01 14:00:00', N'Производство'),
    (N'manager.volodin',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Володин Андрей Сергеевич',     N'manager',      N'andrey.volodin@agrocontrol.ru',    N'+7 (495) 123-45-51', 1, '2025-03-10 10:00:00', '2024-01-10 09:00:00', N'Управление производством'),
    (N'analyst.korolev',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Королев Евгений Павлович',     N'analyst',      N'evgeny.korolev@agrocontrol.ru',    N'+7 (495) 123-45-61', 1, '2025-03-09 12:00:00', '2024-04-18 15:00:00', N'Аналитический отдел'),
    (N'observer.zakharov',N'$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku',N'Захаров Григорий Фёдорович', N'observer',     N'grigory.zakharov@agrocontrol.ru',  N'+7 (495) 123-45-71', 1, '2025-03-08 13:20:00', '2024-08-25 10:00:00', N'Отдел качества')
) AS u(username, password_hash, full_name, role_name, email, phone, is_active, last_login, created_at, dept_name)
JOIN roles r ON r.name = u.role_name
JOIN departments d ON d.name = u.dept_name;

INSERT INTO products (name, type, form, status) VALUES
(N'Гербицид А', N'Гербицид', N'Жидкость', 'active'),
(N'Инсектицид Б', N'Инсектицид', N'Эмульсия', 'active'),
(N'Фунгицид В', N'Фунгицид', N'Порошок', 'active'),
(N'Гербицид Ж', N'Гербицид', N'Жидкость', 'draft'),
(N'Фунгицид Г', N'Фунгицид', N'Гранулы', 'active'),
(N'Регулятор роста Д', N'Регулятор роста', N'Жидкость', 'active'),
(N'Инсектицид Е', N'Инсектицид', N'Эмульсия', 'active'),
(N'Фунгицид З', N'Фунгицид', N'Порошок', 'active'),
(N'Протравитель И', N'Протравитель', N'Концентрат', 'active'),
(N'Гербицид К', N'Гербицид', N'Жидкость', 'archived'),
(N'Фунгицид Л', N'Фунгицид', N'Суспензия', 'active'),
(N'Инсектицид М', N'Инсектицид', N'Эмульсия', 'draft'),
(N'Регулятор роста Н', N'Регулятор роста', N'Раствор', 'active'),
(N'Гербицид О', N'Гербицид', N'Жидкость', 'active'),
(N'Фунгицид П', N'Фунгицид', N'Порошок', 'active'),
(N'Инсектицид Р', N'Инсектицид', N'Эмульсия', 'active'),
(N'Протравитель С', N'Протравитель', N'Концентрат', 'draft');

INSERT INTO raw_materials (name, unit, category) VALUES
(N'Атразин', N'кг', N'Действующее вещество'),
(N'Циперметрин', N'л', N'Действующее вещество'),
(N'Манкоцеб', N'кг', N'Действующее вещество'),
(N'Эмульгатор ОП-10', N'кг', N'Вспомогательное вещество'),
(N'Растворитель нефтяной', N'л', N'Растворитель'),
(N'Вода очищенная', N'л', N'Растворитель'),
(N'Диоксид кремния', N'кг', N'Наполнитель');

-- Рецепты (явные ID)
SET IDENTITY_INSERT recipes ON;
INSERT INTO recipes (id, product_id, version, status, created_at, created_by) VALUES
(1, (SELECT id FROM products WHERE name=N'Гербицид А'), 1, 'active', '2025-01-10 09:00:00', 1),
(2, (SELECT id FROM products WHERE name=N'Инсектицид Б'), 2, 'active', '2025-01-12 10:30:00', 1),
(3, (SELECT id FROM products WHERE name=N'Фунгицид В'), 1, 'draft', '2025-01-15 14:00:00', 1),
(4, (SELECT id FROM products WHERE name=N'Гербицид А'), 2, 'draft', '2025-01-18 11:15:00', 1),
(5, (SELECT id FROM products WHERE name=N'Инсектицид Б'), 1, 'archived', '2025-01-20 08:45:00', 1),
(6, (SELECT id FROM products WHERE name=N'Фунгицид Г'), 1, 'active', '2025-01-22 13:20:00', 2),
(7, (SELECT id FROM products WHERE name=N'Регулятор роста Д'), 1, 'active', '2025-01-25 09:30:00', 2),
(8, (SELECT id FROM products WHERE name=N'Инсектицид Е'), 3, 'active', '2025-01-28 15:00:00', 2),
(9, (SELECT id FROM products WHERE name=N'Гербицид Ж'), 1, 'draft', '2025-02-01 10:00:00', 3),
(10,(SELECT id FROM products WHERE name=N'Фунгицид З'), 2, 'active', '2025-02-03 12:00:00', 3),
(11,(SELECT id FROM products WHERE name=N'Инсектицид Б'), 3, 'draft', '2025-02-05 14:30:00', 3),
(12,(SELECT id FROM products WHERE name=N'Протравитель И'), 1, 'active', '2025-02-07 09:15:00', 4),
(13,(SELECT id FROM products WHERE name=N'Гербицид К'), 1, 'archived', '2025-02-10 11:45:00', 4),
(14,(SELECT id FROM products WHERE name=N'Фунгицид Л'), 1, 'active', '2025-02-12 08:00:00', 1),
(15,(SELECT id FROM products WHERE name=N'Инсектицид М'), 2, 'draft', '2025-02-14 16:20:00', 2),
(16,(SELECT id FROM products WHERE name=N'Регулятор роста Н'), 1, 'active', '2025-02-17 10:10:00', 2),
(17,(SELECT id FROM products WHERE name=N'Гербицид О'), 1, 'active', '2025-02-19 13:40:00', 3),
(18,(SELECT id FROM products WHERE name=N'Фунгицид П'), 1, 'active', '2025-02-21 09:55:00', 3),
(19,(SELECT id FROM products WHERE name=N'Инсектицид Р'), 2, 'active', '2025-02-23 14:05:00', 4),
(20,(SELECT id FROM products WHERE name=N'Протравитель С'), 1, 'draft', '2025-02-25 11:30:00', 4);
SET IDENTITY_INSERT recipes OFF;

INSERT INTO recipe_components (recipe_id, raw_material_id, percentage, load_order) VALUES
(1, 1, 40, 1), (1, 4, 10, 2), (1, 5, 50, 3),
(4, 1, 45, 1), (4, 4, 10, 2), (4, 5, 45, 3),
(2, 2, 25, 1), (2, 4, 15, 2), (2, 5, 60, 3),
(11, 2, 20, 1), (11, 4, 20, 2), (11, 5, 60, 3),
(6, 3, 50, 1), (6, 6, 30, 2), (6, 7, 20, 3),
(7, 1, 10, 1), (7, 6, 90, 2),
(8, 2, 30, 1), (8, 5, 50, 2), (8, 6, 20, 3),
(10, 3, 60, 1), (10, 6, 40, 2),
(12, 1, 70, 1), (12, 5, 30, 2),
(14, 3, 55, 1), (14, 6, 35, 2), (14, 7, 10, 3),
(16, 1, 15, 1), (16, 6, 85, 2),
(17, 1, 42, 1), (17, 5, 48, 2), (17, 4, 10, 3),
(18, 3, 65, 1), (18, 6, 35, 2),
(19, 2, 35, 1), (19, 5, 55, 2), (19, 6, 10, 3);

-- Технологические карты (явные ID)
SET IDENTITY_INSERT tech_maps ON;
INSERT INTO tech_maps (id, product_id, version, status, created_at, created_by) VALUES
(1, (SELECT id FROM products WHERE name=N'Гербицид А'), 1, 'active', '2025-01-10 10:00:00', 1),
(2, (SELECT id FROM products WHERE name=N'Инсектицид Б'), 1, 'active', '2025-01-12 11:00:00', 1),
(3, (SELECT id FROM products WHERE name=N'Фунгицид В'), 1, 'active', '2025-01-15 15:00:00', 1),
(4, (SELECT id FROM products WHERE name=N'Регулятор роста Д'), 1, 'active', '2025-01-25 10:00:00', 2),
(5, (SELECT id FROM products WHERE name=N'Инсектицид Е'), 1, 'active', '2025-01-28 16:00:00', 2),
(6, (SELECT id FROM products WHERE name=N'Фунгицид Г'), 1, 'active', '2025-01-22 14:00:00', 2),
(7, (SELECT id FROM products WHERE name=N'Гербицид Ж'), 1, 'draft', '2025-02-01 11:00:00', 3),
(8, (SELECT id FROM products WHERE name=N'Фунгицид З'), 1, 'active', '2025-02-03 13:00:00', 3),
(9, (SELECT id FROM products WHERE name=N'Протравитель И'), 1, 'active', '2025-02-07 10:00:00', 4),
(10, (SELECT id FROM products WHERE name=N'Фунгицид Л'), 1, 'active', '2025-02-12 09:00:00', 1),
(11, (SELECT id FROM products WHERE name=N'Гербицид О'), 1, 'active', '2025-02-19 14:00:00', 3),
(12, (SELECT id FROM products WHERE name=N'Фунгицид П'), 1, 'active', '2025-02-21 10:00:00', 3),
(13, (SELECT id FROM products WHERE name=N'Инсектицид Р'), 1, 'active', '2025-02-23 15:00:00', 4);
SET IDENTITY_INSERT tech_maps OFF;

INSERT INTO tech_map_steps (tech_map_id, step_order, step_name, step_type, planned_temp_c, planned_pressure_bar, planned_duration_min, is_mandatory) VALUES
(1,1,N'Смешивание',N'Смешивание',45,1.5,30,1),
(1,2,N'Выдержка',N'Выдержка',60,2.0,120,1),
(1,3,N'Экструзия',N'Экструзия',80,3.0,45,1),
(1,4,N'Охлаждение',N'Охлаждение',25,1.0,60,1),
(2,1,N'Смешивание',N'Смешивание',45,1.5,30,1),
(2,2,N'Выдержка',N'Выдержка',60,2.0,120,1),
(2,3,N'Экструзия',N'Экструзия',80,3.0,45,1),
(2,4,N'Охлаждение',N'Охлаждение',25,1.0,60,1),
(4,1,N'Смешивание',N'Смешивание',50,1.5,30,1),
(4,2,N'Выдержка',N'Выдержка',65,2.0,110,1),
(4,3,N'Охлаждение',N'Охлаждение',25,1.0,45,1),
(6,1,N'Смешивание',N'Смешивание',48,1.5,35,1),
(6,2,N'Выдержка',N'Выдержка',62,2.0,115,1),
(8,1,N'Смешивание',N'Смешивание',47,1.5,28,1),
(10,1,N'Смешивание',N'Смешивание',52,1.5,30,1),
(10,2,N'Экструзия',N'Экструзия',88,3.5,42,1);

-- Производственные заказы (явные ID)
SET IDENTITY_INSERT production_orders ON;
INSERT INTO production_orders (id, order_number, recipe_id, planned_quantity_kg, status, planned_start_date, created_by) VALUES
(1,N'PO-2401',1,1000,'completed','2025-03-01',1),
(2,N'PO-2402',2,500,'in_progress','2025-03-03',1),
(3,N'PO-2403',4,2000,'planned','2025-03-10',2),
(4,N'PO-2404',1,800,'in_progress','2025-03-04',2),
(5,N'PO-2405',5,300,'completed','2025-03-01',3),
(6,N'PO-2406',6,1500,'planned','2025-03-12',3),
(7,N'PO-2407',3,600,'draft','2025-03-15',4),
(8,N'PO-2408',7,1200,'completed','2025-03-02',4),
(9,N'PO-2409',8,450,'in_progress','2025-03-05',1),
(10,N'PO-2410',2,2500,'planned','2025-03-18',2),
(11,N'PO-2411',9,750,'draft','2025-03-20',3),
(12,N'PO-2412',10,1800,'completed','2025-03-03',4),
(13,N'PO-2413',4,950,'in_progress','2025-03-06',1),
(14,N'PO-2414',11,620,'planned','2025-03-22',2),
(15,N'PO-2415',12,2100,'completed','2025-03-04',3),
(16,N'PO-2416',13,340,'archived','2025-03-01',4),
(17,N'PO-2417',14,890,'in_progress','2025-03-07',1),
(18,N'PO-2418',1,1550,'planned','2025-03-25',2),
(19,N'PO-2419',15,430,'draft','2025-03-28',3),
(20,N'PO-2420',16,670,'completed','2025-03-05',4);
SET IDENTITY_INSERT production_orders OFF;

-- Партии (явные ID)
SET IDENTITY_INSERT batches ON;
INSERT INTO batches (id, batch_number, order_id, recipe_id, tech_map_id, start_time, end_time, status, actual_quantity_kg)
SELECT
    b.id,
    b.batch_number,
    b.order_id,
    po.recipe_id,
    tm.id AS tech_map_id,
    b.start_time,
    b.end_time,
    b.status,
    b.actual_quantity_kg
FROM (
    VALUES
    (1,N'B-2401-01',1,'2025-03-01 08:00:00','2025-03-01 14:30:00','completed',998),
    (2,N'B-2401-02',1,'2025-03-02 08:15:00','2025-03-02 15:00:00','completed',1002),
    (3,N'B-2402-01',2,'2025-03-03 09:00:00',NULL,'running',250),
    (4,N'B-2404-01',4,'2025-03-04 10:00:00',NULL,'running',400),
    (5,N'B-2405-01',5,'2025-03-01 12:00:00','2025-03-01 16:45:00','completed',298),
    (6,N'B-2406-01',6,'2025-03-12 07:30:00',NULL,'planned',0),
    (7,N'B-2408-01',8,'2025-03-02 06:00:00','2025-03-02 13:20:00','completed',1195),
    (8,N'B-2409-01',9,'2025-03-05 08:00:00',NULL,'running',225),
    (9,N'B-2412-01',12,'2025-03-03 05:45:00','2025-03-03 16:10:00','completed',1795),
    (10,N'B-2413-01',13,'2025-03-06 09:30:00',NULL,'running',475),
    (11,N'B-2415-01',15,'2025-03-04 07:00:00','2025-03-04 18:30:00','completed',2098),
    (12,N'B-2417-01',17,'2025-03-07 08:20:00',NULL,'running',445),
    (13,N'B-2420-01',20,'2025-03-05 10:15:00','2025-03-05 17:45:00','completed',668),
    (14,N'B-2403-01',3,'2025-03-10 06:00:00',NULL,'planned',0),
    (15,N'B-2410-01',10,'2025-03-18 07:00:00',NULL,'planned',0),
    (16,N'B-2414-01',14,'2025-03-22 08:00:00',NULL,'planned',0),
    (17,N'B-2418-01',18,'2025-03-25 09:00:00',NULL,'planned',0),
    (18,N'B-2402-02',2,'2025-03-08 09:00:00',NULL,'running',248),
    (19,N'B-2404-02',4,'2025-03-09 08:00:00',NULL,'planned',0),
    (20,N'B-2409-02',9,'2025-03-10 06:30:00',NULL,'planned',0)
) AS b(id, batch_number, order_id, start_time, end_time, status, actual_quantity_kg)
JOIN production_orders po ON po.id = b.order_id
JOIN tech_maps tm ON tm.product_id = (SELECT product_id FROM recipes WHERE id = po.recipe_id) AND tm.status = 'active'
ORDER BY b.id;
SET IDENTITY_INSERT batches OFF;

-- Шаги партий (явные ID)
SET IDENTITY_INSERT batch_steps ON;
INSERT INTO batch_steps (id, batch_id, step_order, step_name, planned_temp_c, actual_temp_c, planned_duration_min, actual_duration_min, planned_pressure_bar, actual_pressure_bar, deviation_flag, operator_comment)
VALUES
(1,1,1,N'Смешивание',45,44.8,30,32,1.5,1.5,0,N'OK'),
(2,1,2,N'Выдержка',60,59.5,120,118,2.0,1.9,0,N'Незначительное отклонение'),
(3,1,3,N'Экструзия',80,78.2,45,47,3.0,2.8,1,N'Температура ниже нормы'),
(4,1,4,N'Охлаждение',25,24.5,60,58,1.0,1.0,0,N'OK'),
(5,2,1,N'Смешивание',45,46.1,30,30,1.5,1.6,1,N'Температура выше нормы'),
(6,2,2,N'Выдержка',60,58.9,120,125,2.0,1.8,1,N'Отклонение по времени'),
(7,2,3,N'Экструзия',80,79.5,45,46,3.0,2.9,1,N'OK'),
(8,2,4,N'Охлаждение',25,24.8,60,61,1.0,1.0,0,N'OK'),
(9,3,1,N'Смешивание',50,49.8,25,25,1.5,1.5,0,N'OK'),
(10,3,2,N'Экструзия',85,84.0,40,40,3.5,3.4,0,N'OK'),
(11,4,1,N'Смешивание',45,44.5,30,33,1.5,1.4,1,N'Задержка загрузки'),
(12,4,2,N'Выдержка',60,59.8,120,118,2.0,2.0,0,N'OK'),
(13,5,1,N'Смешивание',50,49.9,30,31,1.5,1.5,1,N'Незначительное'),
(14,5,2,N'Выдержка',65,64.5,110,112,2.0,1.9,1,N'Отклонение'),
(15,5,3,N'Охлаждение',25,24.9,45,44,1.0,1.0,0,N'OK'),
(16,7,1,N'Смешивание',48,48.0,35,34,1.5,1.5,0,N'OK'),
(17,7,2,N'Выдержка',62,61.5,115,117,2.0,1.9,1,N'OK'),
(18,8,1,N'Смешивание',47,46.8,28,30,1.5,1.4,1,N'OK'),
(19,9,1,N'Смешивание',52,51.5,30,32,1.5,1.5,1,N'OK'),
(20,9,2,N'Экструзия',88,87.0,42,43,3.5,3.3,1,N'OK');
SET IDENTITY_INSERT batch_steps OFF;

-- Партии сырья (явные ID)
SET IDENTITY_INSERT raw_material_batches ON;
INSERT INTO raw_material_batches (id, raw_material_id, batch_number, supplier, received_date, quantity, unit, status) VALUES
(1,1,N'RM-AT-2401',N'ООО ХимСнаб','2025-02-20',500,N'кг','approved'),
(2,2,N'RM-CP-2401',N'ЗАО АгроТех','2025-02-22',200,N'л','approved'),
(3,3,N'RM-MC-2401',N'ООО ХимСнаб','2025-03-01',300,N'кг','approved');
SET IDENTITY_INSERT raw_material_batches OFF;

INSERT INTO tech_maps (product_id, version, status, created_at, created_by)
SELECT id, 1, 'active', GETDATE(), 1
FROM products
WHERE name = N'Регулятор роста Н';
DECLARE @tech_map_id INT = SCOPE_IDENTITY();
SET IDENTITY_INSERT batches ON;
INSERT INTO batches (id, batch_number, order_id, recipe_id, tech_map_id, start_time, end_time, status, actual_quantity_kg)
VALUES (13, N'B-2420-01', 20, 16, @tech_map_id, '2025-03-05 10:15:00', '2025-03-05 17:45:00', 'completed', 668);
SET IDENTITY_INSERT batches OFF;

SET IDENTITY_INSERT quality_controls ON;
INSERT INTO quality_controls (id, batch_id, raw_material_batch_id, analysis_date, sample_type, parameter_name, measured_value, standard_value, unit, result, decision, analyst_id, analyst_comment)
SELECT
    qc.id,
    CASE WHEN qc.sample_type = N'готовая продукция' THEN qc.batch_id ELSE NULL END,
    CASE WHEN qc.sample_type = N'сырье' THEN 1 ELSE NULL END,
    CAST(qc.analysis_date AS DATETIME2(3)),
    qc.sample_type,
    qc.parameter_name,
    qc.measured_value,
    qc.standard_value,
    qc.unit,
    qc.result,
    qc.decision,
    10,
    qc.analyst_comment
FROM (
    VALUES
    (1,1,'2025-03-01 15:00:00',N'готовая продукция',N'концентрация',98.2,N'97',N'%','pass','approved',N'Соответствует норме'),
    (2,1,'2025-03-01 08:30:00',N'сырье',N'влажность',2.1,N'2.5',N'%','pass','approved',N'OK'),
    (3,2,'2025-03-02 15:30:00',N'готовая продукция',N'концентрация',96.5,N'97',N'%','fail','blocked',N'Низкая концентрация'),
    (4,2,'2025-03-02 08:45:00',N'сырье',N'pH',6.8,N'6.5-7.0',N'','pass','approved',N'OK'),
    (5,3,'2025-03-03 10:00:00',N'сырье',N'pH',6.7,N'6.5-7.0',N'','pass','approved',N'OK'),
    (6,4,'2025-03-04 11:00:00',N'сырье',N'влажность',2.3,N'2.5',N'%','pass','approved',N'OK'),
    (7,5,'2025-03-01 17:00:00',N'готовая продукция',N'концентрация',97.3,N'97',N'%','pass','approved',N'OK'),
    (8,5,'2025-03-01 12:30:00',N'сырье',N'pH',6.9,N'6.5-7.0',N'','pass','approved',N'OK'),
    (9,7,'2025-03-02 14:00:00',N'готовая продукция',N'концентрация',98.5,N'97',N'%','pass','approved',N'Отлично'),
    (10,7,'2025-03-02 06:30:00',N'сырье',N'влажность',1.8,N'2.5',N'%','pass','approved',N'OK'),
    (11,8,'2025-03-05 09:00:00',N'сырье',N'pH',6.6,N'6.5-7.0',N'','pass','approved',N'OK'),
    (12,9,'2025-03-03 16:30:00',N'готовая продукция',N'концентрация',97.8,N'97',N'%','pass','approved',N'OK'),
    (13,9,'2025-03-03 06:00:00',N'сырье',N'влажность',2.0,N'2.5',N'%','pass','approved',N'OK'),
    (14,11,'2025-03-04 19:00:00',N'готовая продукция',N'концентрация',98.9,N'97',N'%','pass','approved',N'Отлично'),
    (15,11,'2025-03-04 07:30:00',N'сырье',N'pH',6.8,N'6.5-7.0',N'','pass','approved',N'OK'),
    (16,13,'2025-03-05 18:00:00',N'готовая продукция',N'концентрация',97.1,N'97',N'%','pass','approved',N'OK'),
    (17,13,'2025-03-05 10:45:00',N'сырье',N'влажность',2.2,N'2.5',N'%','pass','approved',N'OK'),
    (18,3,'2025-03-03 15:00:00',N'готовая продукция',N'концентрация',97.4,N'97',N'%','pass','approved',N'OK'),
    (19,4,'2025-03-04 16:00:00',N'готовая продукция',N'концентрация',97.0,N'97',N'%','pass','approved',N'На границе допуска'),
    (20,8,'2025-03-06 10:00:00',N'готовая продукция',N'концентрация',96.8,N'97',N'%','fail','blocked',N'Требуется переработка')
) AS qc(id, batch_id, analysis_date, sample_type, parameter_name, measured_value, standard_value, unit, result, decision, analyst_comment);
SET IDENTITY_INSERT quality_controls OFF;

SET IDENTITY_INSERT audit_log ON;
INSERT INTO audit_log (id, table_name, record_id, action, new_value, changed_by) VALUES
(1,N'roles',1,N'INSERT',N'{"name":"technologist"}',13),
(2,N'departments',1,N'INSERT',N'{"name":"Технологический отдел"}',13),
(3,N'users',1,N'INSERT',N'{"username":"tech.ivanov"}',13);
SET IDENTITY_INSERT audit_log OFF;
GO

-- ============================================================
-- Проверка успешности вставок (вывод количества записей)
-- ============================================================
SELECT 'roles' AS TableName, COUNT(*) AS RowsCount FROM roles
UNION ALL SELECT 'departments', COUNT(*) FROM departments
UNION ALL SELECT 'users', COUNT(*) FROM users
UNION ALL SELECT 'products', COUNT(*) FROM products
UNION ALL SELECT 'raw_materials', COUNT(*) FROM raw_materials
UNION ALL SELECT 'recipes', COUNT(*) FROM recipes
UNION ALL SELECT 'recipe_components', COUNT(*) FROM recipe_components
UNION ALL SELECT 'tech_maps', COUNT(*) FROM tech_maps
UNION ALL SELECT 'tech_map_steps', COUNT(*) FROM tech_map_steps
UNION ALL SELECT 'production_orders', COUNT(*) FROM production_orders
UNION ALL SELECT 'batches', COUNT(*) FROM batches
UNION ALL SELECT 'batch_steps', COUNT(*) FROM batch_steps
UNION ALL SELECT 'raw_material_batches', COUNT(*) FROM raw_material_batches
UNION ALL SELECT 'quality_controls', COUNT(*) FROM quality_controls
UNION ALL SELECT 'audit_log', COUNT(*) FROM audit_log;