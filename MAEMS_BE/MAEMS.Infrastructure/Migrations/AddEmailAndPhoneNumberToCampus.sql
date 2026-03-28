-- Migration: Add email and phone_number columns to campus table
-- Date: 2024
-- Description: Adds email and phone_number fields to the campus table for contact information

BEGIN;

-- Add email column if it doesn't exist
ALTER TABLE campus
ADD COLUMN IF NOT EXISTS email VARCHAR(150);

-- Add phone_number column if it doesn't exist
ALTER TABLE campus
ADD COLUMN IF NOT EXISTS phone_number VARCHAR(50);

-- Optional: Add comments to columns for documentation
COMMENT ON COLUMN campus.email IS 'Campus contact email address';
COMMENT ON COLUMN campus.phone_number IS 'Campus contact phone number';

COMMIT;
