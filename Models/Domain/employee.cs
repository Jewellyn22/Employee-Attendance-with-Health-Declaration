using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EmployeeAttendanceWithHealthDeclaration.Models.Domain
{
    public class employee
    {
        // Auto-generated on create as {provider_code}-NNNNNN (e.g., ACI-000001; 6-digit
        // sequence per provider/company). Not required on the inbound create payload;
        // validated by existence on update.
        [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
        public string employee_id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(250, ErrorMessage = "Name cannot exceed 250 characters")]
        public string name { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
        [RegularExpression("^(Male|Female)$", ErrorMessage = "Gender must be Male or Female")]
        public string gender { get; set; }

        [Required(ErrorMessage = "Birthdate is required")]
        [DataType(DataType.Date)]
        public DateTime? birthdate { get; set; }

        [StringLength(15, ErrorMessage = "Contact number cannot exceed 15 characters")]
        [Phone(ErrorMessage = "Contact number must be a valid phone number")]
        public string contact_number { get; set; }
        public string address { get; set; }

        [Required(ErrorMessage = "Provider code is required")]
        [StringLength(50, ErrorMessage = "Provider code cannot exceed 50 characters")]
        public string provider_code { get; set; }

        // Comma-separated project codes the employee is assigned to (multi-project;
        // every code must belong to the employee's provider). READ-DERIVED ONLY --
        // returned by reads as a joined CSV (kiosk/dashboard/audit still consume it);
        // create/update payloads carry assignments instead. Never sent to the SP.
        // Nullable: absent from inbound payloads, and a non-nullable string would trip
        // MVC's implicit [Required] during model binding ("field is required").
        public string? project_codes { get; set; }

        // Per-project assignments (the position is specific to each project).
        // Inbound on create/update payloads (admin add-row picker / bulk enrollment);
        // also hydrated from project_positions in the repository when a read entity
        // is re-saved (project deactivation/reactivation cascade paths). Serialized
        // to the p_projects JSON param of sp_employee_Create/_Update.
        // Validated in ValidationHelper.
        public List<employee_project_assignment>? assignments { get; set; }

        // JOIN-derived display fields (populated by the employee read SPs via the
        // employee_project junction, never written). Nullable/0-defaulted so they
        // don't trip implicit [Required] when absent from the create/update payload.
        public string? provider_name { get; set; }
        public string? project_names { get; set; }          // CSV of assigned project names
        public string? areas { get; set; }                  // CSV (DISTINCT) of assigned projects' area_of_destination
        public string? positions { get; set; }              // CSV of per-project positions ('' entries skipped)
        public string? project_positions { get; set; }      // JSON object {"project_code":"position"} (edit modal / hydration source)
        // Newline-joined "ProjectName (Position - Area)" per assigned project --
        // admin Employees table + Excel export display only. READ-DERIVED ONLY
        // (sp_employee_GetAll); never written, never audited.
        public string? project_details { get; set; }

        // JSON array string of per-project display rows [{ "project_name", "area", "position" }]
        // ordered by project_code -- kiosk Personal Detail table only (Home/Scan payload).
        // READ-DERIVED ONLY (sp_employee_GetByEmployeeId); never written, never audited.
        // [JsonIgnore] keeps it out of audit data_from/data_to snapshots (Update/merge/delete pass
        // whole GetByEmployeeId entities to IAuditLogService). Dapper still maps it (JSON attributes
        // are ignored), and the kiosk payload re-projects it manually in the controller.
        [JsonIgnore]
        public string? project_rows { get; set; }
        public int active_project_count { get; set; }       // assigned projects with active=1, is_deleted=0 (scan gate)
        public int? other_active_project_count { get; set; } // only from sp_employee_GetByProjectCode (cascade skip)

        public int active { get; set; }

        // Soft-delete flag: 1 = deleted together with its project (hidden everywhere, row retained).
        public int is_deleted { get; set; }
        public DateTime create_at { get; set; }
        public DateTime update_at { get; set; }
    }
}