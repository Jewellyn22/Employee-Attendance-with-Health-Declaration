Employee Attendance with Health Declaration – Narrative Flow
Overview

This process manages contractor attendance through ID scanning, time-in/time-out recording, and health declaration validation. The system determines whether the scan is a first-time entry, a time-out transaction, or an invalid repeated scan. Health declarations are required for first entries and may require a waiver approval if the contractor is declared unhealthy.

Step-by-Step Narrative
1. Contractor Scans ID

The process begins when a contractor scans their ID card.

2. Determine if it is the First Scan

The system checks whether the contractor has already timed in for the current attendance cycle.

If YES (First Scan)

The contractor is entering the premises for the first time.

Display the Health Declaration Form.
Contractor completes the health declaration.
System evaluates the declaration.
If Contractor is Healthy
Save Time-In record.
Save Health Declaration as Healthy.
Display success message.
Allow contractor to enter the premises.
End process.
If Contractor is Unhealthy
Save Time-In record.
Save Health Declaration as Unhealthy.
Require the contractor's Safety Officer to submit a waiver.
Save waiver information.
Display success message.
Allow contractor entry based on approved waiver.
End process.
If NO (Not First Scan)

The system checks whether the scan occurred within 15 minutes of the previous scan.

If Scan is Less Than 15 Minutes from Previous Scan

Display error message:

"Already Time In/Out"

End process.
If Scan is More Than 15 Minutes from Previous Scan

The system checks for an active attendance session.

3. Check for Open Time-In Session
If an Open Session Exists

The contractor is timing out.

Save Time-Out record.

Display success message:

"Successful Time Out"

End process.
If No Open Session Exists

The system determines whether the contractor's previous health declaration is still valid.

4. Validate Previous Health Declaration

The system checks whether the most recent health declaration was completed within the last 12 hours.

If Previous Health Declaration is Less Than 12 Hours Old
Save Time-In without requiring a new Health Declaration.

Display success message:

"Successful Time In"

End process.
If Previous Health Declaration is More Than 12 Hours Old
Return to Health Declaration process.
Display Health Declaration Form.
Contractor completes declaration.
Continue through the Healthy/Unhealthy validation process.


----
Console Flowchart Diagram

+------------------+
|      START       |
+------------------+
          |
          v
+------------------+
|     SCAN ID      |
+------------------+
          |
          v
+------------------+
|   1ST SCAN ?     |
+------------------+
     |YES     |NO
     |        |
     v        v
+-------------------+      +---------------------------+
| SHOW HEALTH       |      | SCAN < 15 MINS FROM      |
| DECLARATION FORM  |      | PREVIOUS SCAN ?          |
+-------------------+      +---------------------------+
          |                      |YES          |NO
          v                      |             |
+-------------------+            v             v
| CONTRACTOR FILLS  |   +----------------+ +------------------+
| HEALTH DECLARATION|   | ERROR MESSAGE: | | CHECK OPEN      |
+-------------------+   | ALREADY TIME   | | SESSION         |
          |             | IN / OUT       | +------------------+
          v             +----------------+         |
+-------------------+             |               v
|    HEALTHY ?      |             |      +------------------+
+-------------------+             |      | OPEN SESSION ?   |
    |YES      |NO                 |      +------------------+
    |         |                   |         |YES      |NO
    v         v                   |         |         |
+-----------+  +----------------+ |         v         v
| SAVE      |  | SAVE TIME IN   | |  +-------------+ +-------------------+
| TIME IN   |  | AS UNHEALTHY   | |  | SAVE TIME  | | HEALTH DECLARATION|
| HEALTHY   |  +----------------+ |  | OUT        | | < 12 HOURS OLD ? |
+-----------+          |           |  +-------------+ +-------------------+
     |                 v           |         |           |YES      |NO
     |      +-------------------+  |         v           |         |
     |      | SAFETY OFFICER    |  |  +-------------+    v         |
     |      | SUBMITS WAIVER    |  |  | SUCCESSFUL | +------------------+
     |      +-------------------+  |  | TIME OUT   | | SAVE TIME IN    |
     |                 |           |  +-------------+ | WITHOUT NEW HD  |
     |                 v           |         |         +------------------+
     |      +-------------------+  |         |                 |
     |      | SAVE WAIVER INFO  |  |         |                 v
     |      +-------------------+  |         |       +------------------+
     |                 |           |         |       | SUCCESSFUL       |
     |                 v           |         |       | TIME IN          |
     |      +-------------------+  |         |       +------------------+
     |      | SUCCESS MESSAGE   |  |         |                 |
     |      | ALLOW ENTRY       |  |         |                 |
     |      +-------------------+  |         |                 |
     +-------------+---------------+---------+-----------------+
                   |
                   v
            +-------------+
            |     END     |
            +-------------+


Business Rules Summary
        Rule	                                                    Action
First scan of attendance cycle	                            Require Health Declaration
Healthy declaration	                                        Save Time-In and allow entry
Unhealthy declaration	                                    Save Time-In, require waiver approval
Scan within 15 minutes of previous scan	                    Reject and show error
Open attendance session exists	                            Record Time-Out
No open session and health declaration < 12 hours old	    Time-In without new declaration
No open session and health declaration ≥ 12 hours old	    Require new Health Declaration