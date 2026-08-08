using System.ComponentModel.DataAnnotations;

namespace ContractorAttendanceWithHealthDeclaration.Models.Domain
{
    public class contractor_employee
    {
        // Auto-generated on create as {project_code}-NNNN (e.g., ACI-26-01-0001).
        // Not required on the inbound create payload; validated by existence on update.
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
        public string area_of_destination { get; set; }

        [Required(ErrorMessage = "Project code is required")]
        [StringLength(50, ErrorMessage = "Project code cannot exceed 50 characters")]
        public string project_code { get; set; }

        [Required(ErrorMessage = "Provider code is required")]
        [StringLength(50, ErrorMessage = "Provider code cannot exceed 50 characters")]
        public string provider_code { get; set; }

        // JOIN-derived display fields (populated by sp_contractor_employee_GetAll, never written).
        // Nullable so they don't trip implicit [Required] when absent from the create/update payload.
        public string? provider_name { get; set; }
        public string? project_name { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [StringLength(255, ErrorMessage = "Position cannot exceed 255 characters")]
        public string position { get; set; }
        public int active { get; set; }
        public DateTime create_at { get; set; }
        public DateTime update_at { get; set; }
    }
}