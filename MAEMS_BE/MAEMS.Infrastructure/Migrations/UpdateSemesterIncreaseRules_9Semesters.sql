-- =============================================
-- UPDATE SEMESTER INCREASE RULES FOR REGULAR FEES (9 SEMESTERS)
-- =============================================
-- Apply formula: HK4 increases 6.3% from HK1, HK7 increases 6.5% from HK4
-- This applies to ALL regular tuition fees across all campuses
-- FPT University has 9 semesters (HK1-HK9)
-- =============================================

UPDATE tuition_fees
SET 
	semester_increase_rules = 'HK1-3: base amount | HK4-6: +6.3% from HK1 | HK7-9: +6.5% from HK4',
	updated_at = CURRENT_TIMESTAMP
WHERE fee_type = 'REGULAR' AND is_active = true;

-- Verify the update
SELECT 
	campus_name,
	major_name,
	region,
	base_amount,
	semester_increase_rules
FROM tuition_fees
WHERE fee_type = 'REGULAR'
LIMIT 10;

-- =============================================
-- CALCULATION EXAMPLES (9 SEMESTERS)
-- =============================================

-- Example 1: CNTT Hà Nội KV1 (Base: 22,120,000 VND)
-- ─────────────────────────────────────────────────
-- HK1-3: 22,120,000 × 3 = 66,360,000
-- HK4-6: 22,120,000 × 1.063 × 3 = 70,540,680
-- HK7-9: 22,120,000 × 1.063 × 1.065 × 3 = 75,125,823
-- TOTAL 9 semesters: 212,026,503 VND

-- Example 2: CNTT Đà Nẵng KV1 (Base: 15,484,000 - 30% discount)
-- ─────────────────────────────────────────────────
-- HK1-3: 15,484,000 × 3 = 46,452,000
-- HK4-6: 15,484,000 × 1.063 × 3 = 49,378,476
-- HK7-9: 15,484,000 × 1.063 × 1.065 × 3 = 52,588,077
-- TOTAL 9 semesters: 148,418,553 VND

-- Example 3: CNTT Quy Nhơn KV1 (Base: 11,060,000 - 50% discount)
-- ─────────────────────────────────────────────────
-- HK1-3: 11,060,000 × 3 = 33,180,000
-- HK4-6: 11,060,000 × 1.063 × 3 = 35,270,340
-- HK7-9: 11,060,000 × 1.063 × 1.065 × 3 = 37,562,912
-- TOTAL 9 semesters: 106,013,252 VND

-- =============================================
-- FORMULA BREAKDOWN (9 SEMESTERS)
-- =============================================
-- Semester 1: Base Amount
-- Semester 2: Base Amount
-- Semester 3: Base Amount
-- ─── Price increase +6.3% ───
-- Semester 4: Base Amount × 1.063
-- Semester 5: Base Amount × 1.063
-- Semester 6: Base Amount × 1.063
-- ─── Price increase +6.5% ───
-- Semester 7: Base Amount × 1.063 × 1.065
-- Semester 8: Base Amount × 1.063 × 1.065
-- Semester 9: Base Amount × 1.063 × 1.065

-- Total formula for 9 semesters:
-- = (Base × 3) + (Base × 1.063 × 3) + (Base × 1.063 × 1.065 × 3)
-- = Base × (3 + 3.189 + 3.396)
-- = Base × 9.585
-- Example: 22,120,000 × 9.585 ≈ 212,014,200 VND

-- =============================================
-- PRICE INCREASE PERCENTAGES
-- =============================================
-- HK1 → HK4: +6.3%
-- HK4 → HK7: +6.5%
-- HK1 → HK7: +13.2195% (compound: 1.063 × 1.065 = 1.132195)

-- =============================================
-- VERIFICATION QUERIES
-- =============================================

-- 1. Count updated records
SELECT COUNT(*) as updated_regular_fees
FROM tuition_fees
WHERE fee_type = 'REGULAR' 
  AND semester_increase_rules LIKE '%HK7-9:%'
  AND is_active = true;
-- Expected: 390 records

-- 2. Check that non-REGULAR fees are NOT affected
SELECT 
	fee_type,
	COUNT(*) as count,
	MAX(semester_increase_rules) as sample_rule
FROM tuition_fees
WHERE fee_type IN ('ORIENTATION', 'ENGLISH_PREP')
GROUP BY fee_type;
-- Expected: 
-- ORIENTATION: 10 records, rule = 'Fixed fee for orientation semester'
-- ENGLISH_PREP: 10 records, rule = 'Per level fee, maximum 6 levels'

-- 3. Sample calculation verification
SELECT 
	campus_name,
	major_name,
	region,
	base_amount,
	ROUND(base_amount * 9.585) as estimated_9_semester_total,
	semester_increase_rules
FROM tuition_fees
WHERE fee_type = 'REGULAR'
  AND major_name = 'Công nghệ thông tin'
  AND region = 'KV1'
ORDER BY campus_name;

-- 4. Compare campus pricing
SELECT 
	campus_name,
	AVG(base_amount) as avg_base,
	ROUND(AVG(base_amount * 9.585)) as avg_total_9_semesters
FROM tuition_fees
WHERE fee_type = 'REGULAR' AND region = 'KV1'
GROUP BY campus_name
ORDER BY avg_base DESC;

-- =============================================
-- NOTES
-- =============================================
-- • This update only affects REGULAR tuition fees
-- • ORIENTATION and ENGLISH_PREP fees remain fixed per occurrence
-- • The 9.585 multiplier is an approximation for quick estimates
-- • Exact calculations should use the step-by-step formula
-- • All monetary values are in Vietnamese Dong (VND)
