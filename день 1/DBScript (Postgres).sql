-- ============================================================
-- Создание базы данных (пример для PostgreSQL)
-- ============================================================
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE departments (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id),
    email VARCHAR(100),
    phone VARCHAR(30),
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_login TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    department_id INT REFERENCES departments(id)
);

CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    type VARCHAR(50),          -- Гербицид, Инсектицид и т.п.
    form VARCHAR(50),          -- Жидкость, Эмульсия, Порошок
    status VARCHAR(20) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','active','archived'))
);

CREATE TABLE raw_materials (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    unit VARCHAR(20) NOT NULL,  -- кг, л, %
    category VARCHAR(50)       -- Активное вещество, Наполнитель, Растворитель
);

CREATE TABLE recipes (
    id SERIAL PRIMARY KEY,
    product_id INT NOT NULL REFERENCES products(id),
    version INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','active','archived')),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    approved_at TIMESTAMP,
    created_by INT REFERENCES users(id),
    UNIQUE(product_id, version)   -- одна версия на продукт с уникальным номером
);

CREATE TABLE recipe_components (
    id SERIAL PRIMARY KEY,
    recipe_id INT NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    raw_material_id INT NOT NULL REFERENCES raw_materials(id),
    percentage DECIMAL(5,2) NOT NULL CHECK (percentage > 0 AND percentage <= 100),
    load_order INT NOT NULL DEFAULT 0,
    tolerance_min DECIMAL(5,2) DEFAULT 0,
    tolerance_max DECIMAL(5,2) DEFAULT 0
);

CREATE TABLE tech_maps (
    id SERIAL PRIMARY KEY,
    product_id INT NOT NULL REFERENCES products(id),
    version INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','active','archived')),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    created_by INT REFERENCES users(id),
    UNIQUE(product_id, version)
);

CREATE TABLE tech_map_steps (
    id SERIAL PRIMARY KEY,
    tech_map_id INT NOT NULL REFERENCES tech_maps(id) ON DELETE CASCADE,
    step_order INT NOT NULL,
    step_name VARCHAR(100) NOT NULL,
    step_type VARCHAR(50) NOT NULL,   -- Смешивание, Выдержка, Экструзия, Охлаждение
    planned_temp_c DECIMAL(5,1),
    planned_pressure_bar DECIMAL(4,1),
    planned_duration_min INT,
    is_mandatory BOOLEAN NOT NULL DEFAULT true,
    instruction TEXT
);

CREATE TABLE production_orders (
    id SERIAL PRIMARY KEY,
    order_number VARCHAR(20) NOT NULL UNIQUE,
    recipe_id INT NOT NULL REFERENCES recipes(id),
    planned_quantity_kg DECIMAL(10,2) NOT NULL CHECK (planned_quantity_kg > 0),
    status VARCHAR(20) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','planned','in_progress','completed','archived')),
    planned_start_date DATE,
    created_by INT REFERENCES users(id)
);

CREATE TABLE batches (
    id SERIAL PRIMARY KEY,
    batch_number VARCHAR(20) NOT NULL UNIQUE,
    order_id INT NOT NULL REFERENCES production_orders(id),
    recipe_id INT NOT NULL REFERENCES recipes(id),
    tech_map_id INT NOT NULL REFERENCES tech_maps(id),
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'planned' CHECK (status IN ('planned','running','completed','aborted')),
    actual_quantity_kg DECIMAL(10,2) CHECK (actual_quantity_kg >= 0)
);

CREATE TABLE batch_steps (
    id SERIAL PRIMARY KEY,
    batch_id INT NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
    step_order INT NOT NULL,
    step_name VARCHAR(100) NOT NULL,
    planned_temp_c DECIMAL(5,1),
    actual_temp_c DECIMAL(5,1),
    planned_duration_min INT,
    actual_duration_min INT,
    planned_pressure_bar DECIMAL(4,1),
    actual_pressure_bar DECIMAL(4,1),
    started_by INT REFERENCES users(id),
    completed_by INT REFERENCES users(id),
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    deviation_flag BOOLEAN NOT NULL DEFAULT false,
    operator_comment TEXT
);

CREATE TABLE raw_material_batches (
    id SERIAL PRIMARY KEY,
    raw_material_id INT NOT NULL REFERENCES raw_materials(id),
    batch_number VARCHAR(50) NOT NULL UNIQUE,
    supplier VARCHAR(100),
    received_date DATE NOT NULL DEFAULT current_date,
    quantity DECIMAL(10,2) NOT NULL CHECK (quantity > 0),
    unit VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','in_analysis','approved','blocked'))
);

CREATE TABLE quality_controls (
    id SERIAL PRIMARY KEY,
    batch_id INT REFERENCES batches(id),
    raw_material_batch_id INT REFERENCES raw_material_batches(id),
    analysis_date TIMESTAMP NOT NULL DEFAULT now(),
    sample_type VARCHAR(50) NOT NULL,   -- сырье, готовая продукция
    parameter_name VARCHAR(100) NOT NULL,
    measured_value DECIMAL(8,2),
    standard_value VARCHAR(50),         -- '97', '6.5-7.0'
    unit VARCHAR(20),
    result VARCHAR(20) CHECK (result IN ('pass','fail')),
    decision VARCHAR(20) CHECK (decision IN ('approved','blocked')),
    analyst_id INT REFERENCES users(id),
    analyst_comment TEXT
);

CREATE TABLE audit_log (
    id SERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    record_id INT NOT NULL,
    action VARCHAR(20) NOT NULL,    -- INSERT, UPDATE, DELETE, STATUS_CHANGE
    old_value JSONB,
    new_value JSONB,
    changed_by INT REFERENCES users(id),
    changed_at TIMESTAMP NOT NULL DEFAULT now()
);

-- ============================================================
-- ТРИГГЕРЫ ДЛЯ БИЗНЕС-ПРАВИЛ
-- ============================================================

-- Правило 1: Только одна активная рецептура на продукт
CREATE OR REPLACE FUNCTION check_single_active_recipe()
RETURNS trigger AS $$
BEGIN
    IF NEW.status = 'active' THEN
        IF EXISTS (
            SELECT 1 FROM recipes
            WHERE product_id = NEW.product_id
              AND id != NEW.id
              AND status = 'active'
        ) THEN
            RAISE EXCEPTION 'Для продукта уже существует действующая рецептура.';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_recipe_single_active
BEFORE INSERT OR UPDATE ON recipes
FOR EACH ROW EXECUTE FUNCTION check_single_active_recipe();

-- Правило 2: Только одна активная технологическая карта на продукт
CREATE OR REPLACE FUNCTION check_single_active_techmap()
RETURNS trigger AS $$
BEGIN
    IF NEW.status = 'active' THEN
        IF EXISTS (
            SELECT 1 FROM tech_maps
            WHERE product_id = NEW.product_id
              AND id != NEW.id
              AND status = 'active'
        ) THEN
            RAISE EXCEPTION 'Для продукта уже существует действующая технологическая карта.';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_techmap_single_active
BEFORE INSERT OR UPDATE ON tech_maps
FOR EACH ROW EXECUTE FUNCTION check_single_active_techmap();

-- Правило 3: Сумма компонентов рецептуры должна быть 100% перед активацией
CREATE OR REPLACE FUNCTION check_recipe_component_sum()
RETURNS trigger AS $$
DECLARE
    total DECIMAL(5,2);
BEGIN
    IF NEW.status = 'active' THEN
        SELECT COALESCE(SUM(percentage), 0) INTO total
        FROM recipe_components
        WHERE recipe_id = NEW.id;
        IF total <> 100.00 THEN
            RAISE EXCEPTION 'Сумма долей компонентов не равна 100% (текущая сумма = %)', total;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_recipe_activation
BEFORE UPDATE OF status ON recipes
FOR EACH ROW
WHEN (OLD.status != 'active' AND NEW.status = 'active')
EXECUTE FUNCTION check_recipe_component_sum();

-- ============================================================
-- НАЧАЛЬНЫЕ ДАННЫЕ (INSERT)
-- ============================================================

-- Роли
INSERT INTO roles (name) VALUES
('technologist'),
('operator'),
('laboratory'),
('admin'),
('engineer'),
('manager'),
('analyst'),
('observer'),
('shift_supervisor');

-- Подразделения
INSERT INTO departments (name) VALUES
('Технологический отдел'),
('Цех №1'),
('Цех №2'),
('Цех №3'),
('Лаборатория контроля качества'),
('Лаборатория сырья'),
('IT-отдел'),
('Инженерный отдел'),
('Управление производством'),
('Аналитический отдел'),
('Отдел качества'),
('Производство');

-- Пользователи (из users.json)
INSERT INTO users (username, password_hash, full_name, role_id, email, phone, is_active, last_login, created_at, department_id)
SELECT 
    u.username,
    u.password_hash,
    u.full_name,
    r.id,
    u.email,
    u.phone,
    u.is_active,
    u.last_login::timestamp,
    u.created_at::timestamp,
    d.id
FROM (
    VALUES
    ('tech.ivanov',   '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Иванов Иван Петрович',         'technologist', 'ivan.ivanov@agrocontrol.ru',         '+7 (495) 123-45-01', true,  '2025-03-10 08:30:00', '2024-01-15 10:00:00', 'Технологический отдел'),
    ('tech.petrova',  '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Петрова Мария Сергеевна',      'technologist', 'maria.petrova@agrocontrol.ru',      '+7 (495) 123-45-02', true,  '2025-03-09 16:45:00', '2024-02-20 11:30:00', 'Технологический отдел'),
    ('tech.smirnov',  '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Смирнов Алексей Викторович',  'technologist', 'alexey.smirnov@agrocontrol.ru',      '+7 (495) 123-45-03', true,  '2025-03-08 11:20:00', '2024-03-10 09:15:00', 'Технологический отдел'),
    ('tech.kuznetsov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Кузнецов Дмитрий Андреевич',  'technologist', 'dmitry.kuznetsov@agrocontrol.ru',    '+7 (495) 123-45-04', false, '2025-02-20 14:10:00', '2024-04-05 13:00:00', 'Технологический отдел'),
    ('operator.zavodov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Заводов Сергей Николаевич',   'operator',     'sergey.zavodov@agrocontrol.ru',    '+7 (495) 123-45-11', true,  '2025-03-10 07:50:00', '2024-05-20 08:00:00', 'Цех №1'),
    ('operator.melnikov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Мельников Андрей Геннадьевич','operator',     'andrey.melnikov@agrocontrol.ru',   '+7 (495) 123-45-12', true,  '2025-03-09 07:30:00', '2024-06-10 09:00:00', 'Цех №1'),
    ('operator.gromov', '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Громов Илья Дмитриевич',       'operator',     'ilya.gromov@agrocontrol.ru',       '+7 (495) 123-45-13', true,  '2025-03-08 22:15:00', '2024-07-15 10:00:00', 'Цех №2'),
    ('operator.volkov', '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Волков Роман Александрович',   'operator',     'roman.volkov@agrocontrol.ru',      '+7 (495) 123-45-14', true,  '2025-03-09 23:40:00', '2024-08-01 11:45:00', 'Цех №2'),
    ('operator.sokolov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Соколов Павел Олегович',        'operator',     'pavel.sokolov@agrocontrol.ru',     '+7 (495) 123-45-15', false, '2025-02-25 08:00:00', '2024-09-10 13:30:00', 'Цех №3'),
    ('lab.vasilieva',  '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Васильева Елена Андреевна',    'laboratory',   'elena.vasilieva@agrocontrol.ru',   '+7 (495) 123-45-21', true,  '2025-03-10 09:15:00', '2024-01-20 10:00:00', 'Лаборатория контроля качества'),
    ('lab.morozova',   '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Морозова Татьяна Игоревна',     'laboratory',   'tatiana.morozova@agrocontrol.ru',  '+7 (495) 123-45-22', true,  '2025-03-09 14:30:00', '2024-03-15 09:30:00', 'Лаборатория контроля качества'),
    ('lab.nikitina',   '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Никитина Ольга Владимировна',  'laboratory',   'olga.nikitina@agrocontrol.ru',     '+7 (495) 123-45-23', true,  '2025-03-08 11:00:00', '2024-05-10 14:00:00', 'Лаборатория сырья'),
    ('admin.sidorov',  '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku', 'Сидоров Константин Петрович',  'admin',        'admin@agrocontrol.ru',             '+7 (495) 123-45-00', true,  '2025-03-10 08:00:00', '2024-01-01 09:00:00', 'IT-отдел'),
    ('engineer.mikhailov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Михайлов Артём Витальевич', 'engineer',    'artem.mikhailov@agrocontrol.ru',   '+7 (495) 123-45-31', true,  '2025-03-09 17:20:00', '2024-02-10 12:00:00', 'Инженерный отдел'),
    ('engineer.belov', '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Белов Денис Сергеевич',        'engineer',     'denis.belov@agrocontrol.ru',       '+7 (495) 123-45-32', false, '2025-02-28 15:40:00', '2024-06-20 11:00:00', 'Инженерный отдел'),
    ('shift.titov',    '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Титов Максим Ильич',           'shift_supervisor','maxim.titov@agrocontrol.ru',      '+7 (495) 123-45-41', true,  '2025-03-10 06:45:00', '2024-03-25 08:30:00', 'Производство'),
    ('shift.frolov',   '$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Фролов Николай Алексеевич',     'shift_supervisor','nikolay.frolov@agrocontrol.ru',   '+7 (495) 123-45-42', true,  '2025-03-09 18:30:00', '2024-07-01 14:00:00', 'Производство'),
    ('manager.volodin','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Володин Андрей Сергеевич',     'manager',      'andrey.volodin@agrocontrol.ru',    '+7 (495) 123-45-51', true,  '2025-03-10 10:00:00', '2024-01-10 09:00:00', 'Управление производством'),
    ('analyst.korolev','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Королев Евгений Павлович',     'analyst',      'evgeny.korolev@agrocontrol.ru',    '+7 (495) 123-45-61', true,  '2025-03-09 12:00:00', '2024-04-18 15:00:00', 'Аналитический отдел'),
    ('observer.zakharov','$2a$10$N9qo8uLOickgx2ZMRZoMy.Mr7wqY6j5JkZ3vP6q4L8x9Y5Q2wE7Ku','Захаров Григорий Фёдорович', 'observer',     'grigory.zakharov@agrocontrol.ru',  '+7 (495) 123-45-71', true,  '2025-03-08 13:20:00', '2024-08-25 10:00:00', 'Отдел качества')
) AS u(username, password_hash, full_name, role_name, email, phone, is_active, last_login, created_at, dept_name)
JOIN roles r ON r.name = u.role_name
JOIN departments d ON d.name = u.dept_name;

-- Продукты (на основе уникальных названий рецептов)
INSERT INTO products (name, type, form, status) VALUES
('Гербицид А', 'Гербицид', 'Жидкость', 'active'),
('Инсектицид Б', 'Инсектицид', 'Эмульсия', 'active'),
('Фунгицид В', 'Фунгицид', 'Порошок', 'active'),
-- добавим остальные косвенно: создадим записи для всех имён из recipe
('Гербицид Ж', 'Гербицид', 'Жидкость', 'draft'),
('Фунгицид Г', 'Фунгицид', 'Гранулы', 'active'),
('Регулятор роста Д', 'Регулятор роста', 'Жидкость', 'active'),
('Инсектицид Е', 'Инсектицид', 'Эмульсия', 'active'),
('Фунгицид З', 'Фунгицид', 'Порошок', 'active'),
('Протравитель И', 'Протравитель', 'Концентрат', 'active'),
('Гербицид К', 'Гербицид', 'Жидкость', 'archived'),
('Фунгицид Л', 'Фунгицид', 'Суспензия', 'active'),
('Инсектицид М', 'Инсектицид', 'Эмульсия', 'draft'),
('Регулятор роста Н', 'Регулятор роста', 'Раствор', 'active'),
('Гербицид О', 'Гербицид', 'Жидкость', 'active'),
('Фунгицид П', 'Фунгицид', 'Порошок', 'active'),
('Инсектицид Р', 'Инсектицид', 'Эмульсия', 'active'),
('Протравитель С', 'Протравитель', 'Концентрат', 'draft');

-- Сырьё (компоненты) – минимальный набор для демонстрации
INSERT INTO raw_materials (name, unit, category) VALUES
('Атразин', 'кг', 'Действующее вещество'),
('Циперметрин', 'л', 'Действующее вещество'),
('Манкоцеб', 'кг', 'Действующее вещество'),
('Эмульгатор ОП-10', 'кг', 'Вспомогательное вещество'),
('Растворитель нефтяной', 'л', 'Растворитель'),
('Вода очищенная', 'л', 'Растворитель'),
('Диоксид кремния', 'кг', 'Наполнитель');

-- Рецепты (recipe.txt) – каждой записи сопоставляем продукт по имени
INSERT INTO recipes (id, product_id, version, status, created_at, created_by) VALUES
(1, (SELECT id FROM products WHERE name='Гербицид А'), 1, 'active', '2025-01-10 09:00:00', 1),
(2, (SELECT id FROM products WHERE name='Инсектицид Б'), 2, 'active', '2025-01-12 10:30:00', 1),
(3, (SELECT id FROM products WHERE name='Фунгицид В'), 1, 'draft', '2025-01-15 14:00:00', 1),
(4, (SELECT id FROM products WHERE name='Гербицид А'), 2, 'active', '2025-01-18 11:15:00', 1),
(5, (SELECT id FROM products WHERE name='Инсектицид Б'), 1, 'archived', '2025-01-20 08:45:00', 1),
(6, (SELECT id FROM products WHERE name='Фунгицид Г'), 1, 'active', '2025-01-22 13:20:00', 2),
(7, (SELECT id FROM products WHERE name='Регулятор роста Д'), 1, 'active', '2025-01-25 09:30:00', 2),
(8, (SELECT id FROM products WHERE name='Инсектицид Е'), 3, 'active', '2025-01-28 15:00:00', 2),
(9, (SELECT id FROM products WHERE name='Гербицид Ж'), 1, 'draft', '2025-02-01 10:00:00', 3),
(10,(SELECT id FROM products WHERE name='Фунгицид З'), 2, 'active', '2025-02-03 12:00:00', 3),
(11,(SELECT id FROM products WHERE name='Инсектицид Б'), 3, 'active', '2025-02-05 14:30:00', 3),
(12,(SELECT id FROM products WHERE name='Протравитель И'), 1, 'active', '2025-02-07 09:15:00', 4),
(13,(SELECT id FROM products WHERE name='Гербицид К'), 1, 'archived', '2025-02-10 11:45:00', 4),
(14,(SELECT id FROM products WHERE name='Фунгицид Л'), 1, 'active', '2025-02-12 08:00:00', 1),
(15,(SELECT id FROM products WHERE name='Инсектицид М'), 2, 'draft', '2025-02-14 16:20:00', 2),
(16,(SELECT id FROM products WHERE name='Регулятор роста Н'), 1, 'active', '2025-02-17 10:10:00', 2),
(17,(SELECT id FROM products WHERE name='Гербицид О'), 1, 'active', '2025-02-19 13:40:00', 3),
(18,(SELECT id FROM products WHERE name='Фунгицид П'), 1, 'active', '2025-02-21 09:55:00', 3),
(19,(SELECT id FROM products WHERE name='Инсектицид Р'), 2, 'active', '2025-02-23 14:05:00', 4),
(20,(SELECT id FROM products WHERE name='Протравитель С'), 1, 'draft', '2025-02-25 11:30:00', 4);

-- Компоненты к рецептам (чтобы для активных сумма была 100%)
-- Для простоты заполним несколько ключевых, чтобы триггер не блокировал.
INSERT INTO recipe_components (recipe_id, raw_material_id, percentage, load_order) VALUES
(1, 1, 40, 1), (1, 4, 10, 2), (1, 5, 50, 3),  -- Гербицид А v1 сумма=100
(4, 1, 45, 1), (4, 4, 10, 2), (4, 5, 45, 3),  -- Гербицид А v2 сумма=100
(2, 2, 25, 1), (2, 4, 15, 2), (2, 5, 60, 3),  -- Инсектицид Б v2 сумма=100
(11, 2, 20, 1), (11, 4, 20, 2), (11, 5, 60, 3),-- Инсектицид Б v3 сумма=100
(6, 3, 50, 1), (6, 6, 30, 2), (6, 7, 20, 3),  -- Фунгицид Г сумма=100
(7, 1, 10, 1), (7, 6, 90, 2),                 -- Регулятор роста Д сумма=100
(8, 2, 30, 1), (8, 5, 50, 2), (8, 6, 20, 3),  -- Инсектицид Е сумма=100
(10, 3, 60, 1), (10, 6, 40, 2),                -- Фунгицид З сумма=100
(12, 1, 70, 1), (12, 5, 30, 2),                -- Протравитель И сумма=100
(14, 3, 55, 1), (14, 6, 35, 2), (14, 7, 10, 3),-- Фунгицид Л сумма=100
(16, 1, 15, 1), (16, 6, 85, 2),                -- Регулятор роста Н сумма=100
(17, 1, 42, 1), (17, 5, 48, 2), (17, 4, 10, 3),-- Гербицид О сумма=100? 42+48+10=100
(18, 3, 65, 1), (18, 6, 35, 2),                -- Фунгицид П сумма=100
(19, 2, 35, 1), (19, 5, 55, 2), (19, 6, 10, 3);-- Инсектицид Р сумма=100

-- Технологические карты (создадим заглушки, чтобы связать партии)
INSERT INTO tech_maps (id, product_id, version, status, created_at, created_by) VALUES
(1, (SELECT id FROM products WHERE name='Гербицид А'), 1, 'active', '2025-01-10 10:00:00', 1),
(2, (SELECT id FROM products WHERE name='Инсектицид Б'), 1, 'active', '2025-01-12 11:00:00', 1),
(3, (SELECT id FROM products WHERE name='Фунгицид В'), 1, 'active', '2025-01-15 15:00:00', 1),
-- добавим ещё для продуктов, используемых в заказах
(4, (SELECT id FROM products WHERE name='Регулятор роста Д'), 1, 'active', '2025-01-25 10:00:00', 2),
(5, (SELECT id FROM products WHERE name='Инсектицид Е'), 1, 'active', '2025-01-28 16:00:00', 2),
(6, (SELECT id FROM products WHERE name='Фунгицид Г'), 1, 'active', '2025-01-22 14:00:00', 2),
(7, (SELECT id FROM products WHERE name='Гербицид Ж'), 1, 'draft', '2025-02-01 11:00:00', 3),
(8, (SELECT id FROM products WHERE name='Фунгицид З'), 1, 'active', '2025-02-03 13:00:00', 3),
(9, (SELECT id FROM products WHERE name='Протравитель И'), 1, 'active', '2025-02-07 10:00:00', 4),
(10, (SELECT id FROM products WHERE name='Фунгицид Л'), 1, 'active', '2025-02-12 09:00:00', 1),
(11, (SELECT id FROM products WHERE name='Гербицид О'), 1, 'active', '2025-02-19 14:00:00', 3),
(12, (SELECT id FROM products WHERE name='Фунгицид П'), 1, 'active', '2025-02-21 10:00:00', 3),
(13, (SELECT id FROM products WHERE name='Инсектицид Р'), 1, 'active', '2025-02-23 15:00:00', 4);

-- Шаги технологических карт (базовые)
INSERT INTO tech_map_steps (tech_map_id, step_order, step_name, step_type, planned_temp_c, planned_pressure_bar, planned_duration_min, is_mandatory) VALUES
-- Для карты 1 (Гербицид А)
(1,1,'Смешивание','Смешивание',45,1.5,30,true),
(1,2,'Выдержка','Выдержка',60,2.0,120,true),
(1,3,'Экструзия','Экструзия',80,3.0,45,true),
(1,4,'Охлаждение','Охлаждение',25,1.0,60,true),
-- Для карты 2 (Инсектицид Б) – похожие шаги
(2,1,'Смешивание','Смешивание',45,1.5,30,true),
(2,2,'Выдержка','Выдержка',60,2.0,120,true),
(2,3,'Экструзия','Экструзия',80,3.0,45,true),
(2,4,'Охлаждение','Охлаждение',25,1.0,60,true),
-- и т.д. для других продуктов (достаточно для импорта)
(4,1,'Смешивание','Смешивание',50,1.5,30,true),
(4,2,'Выдержка','Выдержка',65,2.0,110,true),
(4,3,'Охлаждение','Охлаждение',25,1.0,45,true),
(6,1,'Смешивание','Смешивание',48,1.5,35,true),
(6,2,'Выдержка','Выдержка',62,2.0,115,true),
(8,1,'Смешивание','Смешивание',47,1.5,28,true),
(10,1,'Смешивание','Смешивание',52,1.5,30,true),
(10,2,'Экструзия','Экструзия',88,3.5,42,true);

-- Производственные заказы (production_order.txt)
INSERT INTO production_orders (id, order_number, recipe_id, planned_quantity_kg, status, planned_start_date, created_by) VALUES
(1,'PO-2401',1,1000,'completed','2025-03-01',1),
(2,'PO-2402',2,500,'in_progress','2025-03-03',1),
(3,'PO-2403',4,2000,'planned','2025-03-10',2),
(4,'PO-2404',1,800,'in_progress','2025-03-04',2),
(5,'PO-2405',5,300,'completed','2025-03-01',3),
(6,'PO-2406',6,1500,'planned','2025-03-12',3),
(7,'PO-2407',3,600,'draft','2025-03-15',4),
(8,'PO-2408',7,1200,'completed','2025-03-02',4),
(9,'PO-2409',8,450,'in_progress','2025-03-05',1),
(10,'PO-2410',2,2500,'planned','2025-03-18',2),
(11,'PO-2411',9,750,'draft','2025-03-20',3),
(12,'PO-2412',10,1800,'completed','2025-03-03',4),
(13,'PO-2413',4,950,'in_progress','2025-03-06',1),
(14,'PO-2414',11,620,'planned','2025-03-22',2),
(15,'PO-2415',12,2100,'completed','2025-03-04',3),
(16,'PO-2416',13,340,'archived','2025-03-01',4),
(17,'PO-2417',14,890,'in_progress','2025-03-07',1),
(18,'PO-2418',1,1550,'planned','2025-03-25',2),
(19,'PO-2419',15,430,'draft','2025-03-28',3),
(20,'PO-2420',16,670,'completed','2025-03-05',4);

-- Партии (batch.txt). Некоторые recipe_id и tech_map_id нужно сопоставить.
-- Свяжем заказы с соответствующими картами (возьмём tech_map_id продукта заказа)
-- batch.txt имеет поля: id,batch_number,order_id,start_time,end_time,status,actual_quantity_kg
INSERT INTO batches (id, batch_number, order_id, recipe_id, tech_map_id, start_time, end_time, status, actual_quantity_kg)
SELECT
    b.id,
    b.batch_number,
    b.order_id,
    po.recipe_id,
    tm.id AS tech_map_id,
    b.start_time::timestamp,
    b.end_time::timestamp,
    b.status,
    b.actual_quantity_kg
FROM (VALUES
    (1,'B-2401-01',1,'2025-03-01 08:00:00','2025-03-01 14:30:00','completed',998),
    (2,'B-2401-02',1,'2025-03-02 08:15:00','2025-03-02 15:00:00','completed',1002),
    (3,'B-2402-01',2,'2025-03-03 09:00:00',NULL,'running',250),
    (4,'B-2404-01',4,'2025-03-04 10:00:00',NULL,'running',400),
    (5,'B-2405-01',5,'2025-03-01 12:00:00','2025-03-01 16:45:00','completed',298),
    (6,'B-2406-01',6,'2025-03-12 07:30:00',NULL,'planned',0),
    (7,'B-2408-01',8,'2025-03-02 06:00:00','2025-03-02 13:20:00','completed',1195),
    (8,'B-2409-01',9,'2025-03-05 08:00:00',NULL,'running',225),
    (9,'B-2412-01',12,'2025-03-03 05:45:00','2025-03-03 16:10:00','completed',1795),
    (10,'B-2413-01',13,'2025-03-06 09:30:00',NULL,'running',475),
    (11,'B-2415-01',15,'2025-03-04 07:00:00','2025-03-04 18:30:00','completed',2098),
    (12,'B-2417-01',17,'2025-03-07 08:20:00',NULL,'running',445),
    (13,'B-2420-01',20,'2025-03-05 10:15:00','2025-03-05 17:45:00','completed',668),
    (14,'B-2403-01',3,'2025-03-10 06:00:00',NULL,'planned',0),
    (15,'B-2410-01',10,'2025-03-18 07:00:00',NULL,'planned',0),
    (16,'B-2414-01',14,'2025-03-22 08:00:00',NULL,'planned',0),
    (17,'B-2418-01',18,'2025-03-25 09:00:00',NULL,'planned',0),
    (18,'B-2402-02',2,'2025-03-08 09:00:00',NULL,'running',248),
    (19,'B-2404-02',4,'2025-03-09 08:00:00',NULL,'planned',0),
    (20,'B-2409-02',9,'2025-03-10 06:30:00',NULL,'planned',0)
) AS b(id, batch_number, order_id, start_time, end_time, status, actual_quantity_kg)
JOIN production_orders po ON po.id = b.order_id
JOIN tech_maps tm ON tm.product_id = (SELECT product_id FROM recipes WHERE id = po.recipe_id) AND tm.status = 'active'
ORDER BY b.id;

-- Шаги выполнения партий (production_step.txt)
INSERT INTO batch_steps (id, batch_id, step_order, step_name, planned_temp_c, actual_temp_c, planned_duration_min, actual_duration_min, planned_pressure_bar, actual_pressure_bar, deviation_flag, operator_comment)
VALUES
(1,1,1,'Смешивание',45,44.8,30,32,1.5,1.5,false,'OK'),
(2,1,2,'Выдержка',60,59.5,120,118,2.0,1.9,false,'Незначительное отклонение'),
(3,1,3,'Экструзия',80,78.2,45,47,3.0,2.8,true,'Температура ниже нормы'),
(4,1,4,'Охлаждение',25,24.5,60,58,1.0,1.0,false,'OK'),
(5,2,1,'Смешивание',45,46.1,30,30,1.5,1.6,true,'Температура выше нормы'),
(6,2,2,'Выдержка',60,58.9,120,125,2.0,1.8,true,'Отклонение по времени'),
(7,2,3,'Экструзия',80,79.5,45,46,3.0,2.9,true,'OK'),
(8,2,4,'Охлаждение',25,24.8,60,61,1.0,1.0,false,'OK'),
(9,3,1,'Смешивание',50,49.8,25,25,1.5,1.5,false,'OK'),
(10,3,2,'Экструзия',85,84.0,40,40,3.5,3.4,false,'OK'),
(11,4,1,'Смешивание',45,44.5,30,33,1.5,1.4,true,'Задержка загрузки'),
(12,4,2,'Выдержка',60,59.8,120,118,2.0,2.0,false,'OK'),
(13,5,1,'Смешивание',50,49.9,30,31,1.5,1.5,true,'Незначительное'),
(14,5,2,'Выдержка',65,64.5,110,112,2.0,1.9,true,'Отклонение'),
(15,5,3,'Охлаждение',25,24.9,45,44,1.0,1.0,false,'OK'),
(16,7,1,'Смешивание',48,48.0,35,34,1.5,1.5,false,'OK'),
(17,7,2,'Выдержка',62,61.5,115,117,2.0,1.9,true,'OK'),
(18,8,1,'Смешивание',47,46.8,28,30,1.5,1.4,true,'OK'),
(19,9,1,'Смешивание',52,51.5,30,32,1.5,1.5,true,'OK'),
(20,9,2,'Экструзия',88,87.0,42,43,3.5,3.3,true,'OK');

-- Партии сырья (заглушка, необходимы для лаборатории)
INSERT INTO raw_material_batches (id, raw_material_id, batch_number, supplier, received_date, quantity, unit, status) VALUES
(1,1,'RM-AT-2401','ООО ХимСнаб','2025-02-20',500,'кг','approved'),
(2,2,'RM-CP-2401','ЗАО АгроТех','2025-02-22',200,'л','approved'),
(3,3,'RM-MC-2401','ООО ХимСнаб','2025-03-01',300,'кг','approved');

-- Контроль качества (quality_control.txt)
-- Свяжем batch_id или raw_material_batch_id согласно sample_type
INSERT INTO quality_controls (id, batch_id, raw_material_batch_id, analysis_date, sample_type, parameter_name, measured_value, standard_value, unit, result, decision, analyst_id, analyst_comment)
SELECT
    qc.id,
    CASE WHEN qc.sample_type = 'готовая продукция' THEN qc.batch_id ELSE NULL END,
    CASE WHEN qc.sample_type = 'сырье' THEN 1 ELSE NULL END,  -- заглушка, используем первую партию сырья
    qc.analysis_date::timestamp,
    qc.sample_type,
    qc.parameter_name,
    qc.measured_value,
    qc.standard_value,
    qc.unit,
    qc.result,
    qc.decision,
    10,  -- лаборант Васильева
    qc.analyst_comment
FROM (VALUES
    (1,1,'2025-03-01 15:00:00','готовая продукция','концентрация',98.2,'97','%','pass','approved','Соответствует норме'),
    (2,1,'2025-03-01 08:30:00','сырье','влажность',2.1,'2.5','%','pass','approved','OK'),
    (3,2,'2025-03-02 15:30:00','готовая продукция','концентрация',96.5,'97','%','fail','blocked','Низкая концентрация'),
    (4,2,'2025-03-02 08:45:00','сырье','pH',6.8,'6.5-7.0','','pass','approved','OK'),
    (5,3,'2025-03-03 10:00:00','сырье','pH',6.7,'6.5-7.0','','pass','approved','OK'),
    (6,4,'2025-03-04 11:00:00','сырье','влажность',2.3,'2.5','%','pass','approved','OK'),
    (7,5,'2025-03-01 17:00:00','готовая продукция','концентрация',97.3,'97','%','pass','approved','OK'),
    (8,5,'2025-03-01 12:30:00','сырье','pH',6.9,'6.5-7.0','','pass','approved','OK'),
    (9,7,'2025-03-02 14:00:00','готовая продукция','концентрация',98.5,'97','%','pass','approved','Отлично'),
    (10,7,'2025-03-02 06:30:00','сырье','влажность',1.8,'2.5','%','pass','approved','OK'),
    (11,8,'2025-03-05 09:00:00','сырье','pH',6.6,'6.5-7.0','','pass','approved','OK'),
    (12,9,'2025-03-03 16:30:00','готовая продукция','концентрация',97.8,'97','%','pass','approved','OK'),
    (13,9,'2025-03-03 06:00:00','сырье','влажность',2.0,'2.5','%','pass','approved','OK'),
    (14,11,'2025-03-04 19:00:00','готовая продукция','концентрация',98.9,'97','%','pass','approved','Отлично'),
    (15,11,'2025-03-04 07:30:00','сырье','pH',6.8,'6.5-7.0','','pass','approved','OK'),
    (16,13,'2025-03-05 18:00:00','готовая продукция','концентрация',97.1,'97','%','pass','approved','OK'),
    (17,13,'2025-03-05 10:45:00','сырье','влажность',2.2,'2.5','%','pass','approved','OK'),
    (18,3,'2025-03-03 15:00:00','готовая продукция','концентрация',97.4,'97','%','pass','approved','OK'),
    (19,4,'2025-03-04 16:00:00','готовая продукция','концентрация',97.0,'97','%','pass','approved','На границе допуска'),
    (20,8,'2025-03-06 10:00:00','готовая продукция','концентрация',96.8,'97','%','fail','blocked','Требуется переработка')
) AS qc(id, batch_id, analysis_date, sample_type, parameter_name, measured_value, standard_value, unit, result, decision, analyst_comment);

-- Логирование всех вставок (пример для аудита)
INSERT INTO audit_log (table_name, record_id, action, new_value, changed_by) VALUES
('roles',1,'INSERT','{"name":"technologist"}',13),
('departments',1,'INSERT','{"name":"Технологический отдел"}',13),
('users',1,'INSERT','{"username":"tech.ivanov"}',13);