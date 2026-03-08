# Run this script to create the test validation rules in the system. Make sure the API is running and accessible at the specified URL before executing this script.
$base = "http://localhost:5225/api/validation-rules"
$headers = @{ "Content-Type" = "application/json" }

# 1 — No blank first name
Invoke-RestMethod -Method Post -Uri $base -Headers $headers -Body '{
  "name": "FirstName required",
  "description": "Every record must have a non-empty FirstName in column 2.",
  "ruleText": "Column 2 (FirstName) must not be blank on any data row.",
  "scope": "RECORD",
  "fileType": "CSV",
  "isActive": true
}'

# 2 — Valid email format
Invoke-RestMethod -Method Post -Uri $base -Headers $headers -Body '{
  "name": "Email format",
  "description": "Email column must contain a valid email address.",
  "ruleText": "Column 4 (Email) must match a valid email address format (contains @ and a domain).",
  "scope": "RECORD",
  "fileType": "CSV",
  "isActive": true
}'

# 3 — Valid date format
Invoke-RestMethod -Method Post -Uri $base -Headers $headers -Body '{
  "name": "StartDate format",
  "description": "StartDate must be a valid calendar date in YYYY-MM-DD format.",
  "ruleText": "Column 6 (StartDate) must be a valid date in YYYY-MM-DD format and must represent a real calendar date.",
  "scope": "RECORD",
  "fileType": "CSV",
  "isActive": true
}'

# 4 — Salary is a positive number
Invoke-RestMethod -Method Post -Uri $base -Headers $headers -Body '{
  "name": "Salary positive",
  "description": "Salary must be a positive numeric value.",
  "ruleText": "Column 7 (Salary) must be a positive number greater than zero.",
  "scope": "RECORD",
  "fileType": "ALL",
  "isActive": true
}'

# 5 — Header column count
Invoke-RestMethod -Method Post -Uri $base -Headers $headers -Body '{
  "name": "Header column count",
  "description": "The header row must have exactly 7 columns.",
  "ruleText": "The header row must contain exactly 7 columns.",
  "scope": "HEADER",
  "fileType": "CSV",
  "isActive": true
}'