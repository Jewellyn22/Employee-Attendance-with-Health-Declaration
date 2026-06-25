namespace ContractorAttendanceWithHealthDeclaration.ExternalApis.Ldap.Models
{
    public class ldap_user
    {
        public string office { get; set; }  // Employee ID
        public string displayName { get; set; }
        public string[] member_of { get; set; }
        public string mail { get; set; }
    }
}