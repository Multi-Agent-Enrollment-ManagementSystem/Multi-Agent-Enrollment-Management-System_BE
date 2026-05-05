-- =============================================
-- FIX ĐÀ NẴNG TUITION FEES - CORRECTED PRICING
-- =============================================
-- Issue: Previous script applied 30% discount from Hà Nội prices
-- Reality: Đà Nẵng has FIXED prices different from the discount model
-- 
-- Correct pricing for Đà Nẵng Campus:
-- Group 1 (CNTT, Kinh doanh, Marketing, etc.): KV1 = 15,480,000 | OTHER = 22,120,000
-- Group 2 (Ngôn ngữ, Luật, Du lịch):           KV1 = 10,840,000 | OTHER = 15,480,000
-- =============================================

-- First, delete all existing Đà Nẵng regular tuition fees
DELETE FROM tuition_fees 
WHERE campus_name = 'Đà Nẵng' 
  AND fee_type = 'REGULAR';

-- =============================================
-- GROUP 1: CNTT, Truyền thông, Kinh doanh, Tài chính
-- KV1: 15,480,000 VND | OTHER: 22,120,000 VND
-- =============================================

-- Công nghệ thông tin (9 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
-- Công nghệ thông tin
(1, 'Công nghệ thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí chuyên ngành CNTT tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(1, 'Công nghệ thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí chuyên ngành CNTT tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Kỹ thuật phần mềm
(2, 'Kỹ thuật phần mềm', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Kỹ thuật phần mềm tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, 'Kỹ thuật phần mềm', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Kỹ thuật phần mềm tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Trí tuệ nhân tạo
(3, 'Trí tuệ nhân tạo', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí AI tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(3, 'Trí tuệ nhân tạo', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí AI tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Khoa học dữ liệu và ứng dụng
(4, 'Khoa học dữ liệu và ứng dụng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Data Science tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(4, 'Khoa học dữ liệu và ứng dụng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Data Science tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- An toàn thông tin
(5, 'An toàn thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí An toàn thông tin tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(5, 'An toàn thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí An toàn thông tin tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Vi mạch bán dẫn
(6, 'Vi mạch bán dẫn', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Vi mạch bán dẫn tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(6, 'Vi mạch bán dẫn', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Vi mạch bán dẫn tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Công nghệ ô tô số
(7, 'Công nghệ ô tô số (Automotive)', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Automotive tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(7, 'Công nghệ ô tô số (Automotive)', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Automotive tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Hệ thống thông tin
(8, 'Hệ thống thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Hệ thống thông tin tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(8, 'Hệ thống thông tin', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Hệ thống thông tin tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Thiết kế đồ họa và mỹ thuật số
(NULL, 'Thiết kế đồ họa và mỹ thuật số', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Thiết kế đồ họa tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Thiết kế đồ họa và mỹ thuật số', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Thiết kế đồ họa tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Truyền thông (4 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Truyền thông đa phương tiện', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Truyền thông đa phương tiện tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Truyền thông đa phương tiện', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Truyền thông đa phương tiện tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quan hệ công chúng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quan hệ công chúng tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quan hệ công chúng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quan hệ công chúng tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Truyền thông Marketing tích hợp', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Marketing tích hợp tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Truyền thông Marketing tích hợp', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Marketing tích hợp tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Truyền thông thương hiệu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Truyền thông thương hiệu tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Truyền thông thương hiệu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Truyền thông thương hiệu tại Đà Nẵng', 'Áp dụng cho sinh viên K22 nhập học 2026', '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Kinh doanh & Tài chính (15 majors from table - excluding Du lịch group)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Marketing', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Marketing tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Marketing', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Marketing tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Kinh doanh quốc tế', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Kinh doanh quốc tế tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Kinh doanh quốc tế', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Kinh doanh quốc tế tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Thương mại điện tử', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Thương mại điện tử tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Thương mại điện tử', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Thương mại điện tử tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quản trị kinh doanh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị kinh doanh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị kinh doanh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị kinh doanh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quản trị giải trí và sự kiện', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị giải trí tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị giải trí và sự kiện', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị giải trí tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quản trị trải nghiệm khách hàng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Trải nghiệm khách hàng tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị trải nghiệm khách hàng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Trải nghiệm khách hàng tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quản trị thu mua', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị thu mua tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị thu mua', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị thu mua tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Phân tích kinh doanh (Business Analytics)', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Business Analytics tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Phân tích kinh doanh (Business Analytics)', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Business Analytics tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Logistics và quản lý chuỗi cung ứng toàn cầu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Logistics tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Logistics và quản lý chuỗi cung ứng toàn cầu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Logistics tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Công nghệ tài chính', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Fintech tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Công nghệ tài chính', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Fintech tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tài chính doanh nghiệp', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính doanh nghiệp tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tài chính doanh nghiệp', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính doanh nghiệp tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tài chính thông minh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính thông minh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tài chính thông minh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính thông minh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tài chính Ngân hàng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính Ngân hàng tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tài chính Ngân hàng', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tài chính Ngân hàng tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Cử nhân tài năng (2 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Trí tuệ nhân tạo và Khoa học dữ liệu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí AI & Data Science (Cử nhân tài năng) tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Trí tuệ nhân tạo và Khoa học dữ liệu', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí AI & Data Science (Cử nhân tài năng) tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'An ninh mạng và An toàn số', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Cybersecurity (Cử nhân tài năng) tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'An ninh mạng và An toàn số', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 22120000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Cybersecurity (Cử nhân tài năng) tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- =============================================
-- GROUP 2: NGÔN NGỮ, LUẬT, DU LỊCH
-- KV1: 10,840,000 VND | OTHER: 15,480,000 VND
-- =============================================

-- Ngôn ngữ (6 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Ngôn ngữ Anh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Anh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Ngôn ngữ Anh', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Anh tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tiếng Anh thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Anh thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tiếng Anh thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Anh thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Ngôn ngữ Hàn Quốc', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Hàn Quốc tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Ngôn ngữ Hàn Quốc', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Hàn Quốc tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tiếng Hàn thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Hàn thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tiếng Hàn thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Hàn thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Ngôn ngữ Trung Quốc', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Trung Quốc tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Ngôn ngữ Trung Quốc', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Ngôn ngữ Trung Quốc tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Tiếng Trung thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Trung thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Tiếng Trung thương mại', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Tiếng Trung thương mại tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Luật (2 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Luật', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Luật tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Luật', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Luật tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Luật kinh tế', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Luật kinh tế tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Luật kinh tế', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Luật kinh tế tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Du lịch (2 majors)
INSERT INTO tuition_fees (major_id, major_name, campus_id, campus_name, enrollment_year_id, enrollment_year, region, fee_type, base_amount, campus_discount_percent, semester_increase_rules, currency, description, notes, effective_from, is_active, created_at, updated_at)
VALUES
(NULL, 'Quản trị khách sạn', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị khách sạn tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị khách sạn', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Quản trị khách sạn tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

(NULL, 'Quản trị dịch vụ Du lịch và Lữ hành', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'KV1', 'REGULAR', 10840000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Du lịch và Lữ hành tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(NULL, 'Quản trị dịch vụ Du lịch và Lữ hành', 3, 'Đà Nẵng', 1, 'K22 (2026)', 'OTHER', 'REGULAR', 15480000, 0, 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4', 'VND', 'Học phí Du lịch và Lữ hành tại Đà Nẵng', NULL, '2026-01-01', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- =============================================
-- VERIFICATION QUERIES
-- =============================================

-- Count new records
SELECT COUNT(*) as danang_regular_fees
FROM tuition_fees
WHERE campus_name = 'Đà Nẵng' AND fee_type = 'REGULAR';
-- Expected: 78 records (39 majors × 2 regions)

-- Check pricing distribution
SELECT 
	CASE 
		WHEN base_amount = 15480000 AND region = 'KV1' THEN 'Group 1 - KV1 (CNTT/Business)'
		WHEN base_amount = 22120000 AND region = 'OTHER' THEN 'Group 1 - OTHER (CNTT/Business)'
		WHEN base_amount = 10840000 AND region = 'KV1' THEN 'Group 2 - KV1 (Language/Law/Tourism)'
		WHEN base_amount = 15480000 AND region = 'OTHER' THEN 'Group 2 - OTHER (Language/Law/Tourism)'
		ELSE 'UNEXPECTED'
	END as price_group,
	COUNT(*) as count
FROM tuition_fees
WHERE campus_name = 'Đà Nẵng' AND fee_type = 'REGULAR'
GROUP BY base_amount, region
ORDER BY base_amount DESC, region;

-- Sample records by major group
SELECT major_name, region, base_amount, semester_increase_rules
FROM tuition_fees
WHERE campus_name = 'Đà Nẵng' 
  AND fee_type = 'REGULAR'
  AND major_name IN ('Công nghệ thông tin', 'Ngôn ngữ Anh', 'Luật', 'Marketing')
ORDER BY major_name, region;

-- =============================================
-- NOTES
-- =============================================
-- ✅ Đà Nẵng pricing is FIXED, not discount-based
-- ✅ Group 1 (CNTT/Business): KV1=15.48M, OTHER=22.12M
-- ✅ Group 2 (Language/Law): KV1=10.84M, OTHER=15.48M
-- ✅ Total 78 records (39 majors × 2 regions)
-- ✅ All records have 9-semester increase rules
-- ✅ campus_discount_percent = 0 (no discount model)
