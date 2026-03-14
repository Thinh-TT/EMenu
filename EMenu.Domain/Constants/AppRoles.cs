namespace EMenu.Domain.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Kitchen = "Kitchen";

        public const string AdminOrStaff = Admin + "," + Staff;
        public const string AdminStaffKitchen = Admin + "," + Staff + "," + Kitchen;
    }
}
