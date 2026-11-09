export type UserRole = "Admin" | "Driver" | "Parent";
export type AccountStatus = "Pending" | "Active" | "Suspended" | "Rejected";

export interface DashboardSummary {
  activeDrivers: number;
  registeredStudents: number;
  totalVehicles: number;
}

export interface Driver {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  status: AccountStatus;
}
