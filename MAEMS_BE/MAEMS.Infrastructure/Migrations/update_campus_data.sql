UPDATE campus SET email = 'tuyensinhhanoi@fpt.edu.vn', phone_number = '(024)73005588' WHERE campus_id = 1;
UPDATE campus SET email = 'tuyensinhhcm@fpt.edu.vn', phone_number = '(028)73005588' WHERE campus_id = 2;
UPDATE campus SET email = 'tuyensinhdanang@fpt.edu.vn', phone_number = '(0236)7300999' WHERE campus_id = 3;
UPDATE campus SET email = 'tuyensinhcantho@fpt.edu.vn', phone_number = '(0292)7303636' WHERE campus_id = 4;
UPDATE campus SET email = 'tuyensinhquynhon@fpt.edu.vn', phone_number = '(0256)7300999' WHERE campus_id = 5;
SELECT campus_id, name, email, phone_number FROM campus ORDER BY campus_id;
