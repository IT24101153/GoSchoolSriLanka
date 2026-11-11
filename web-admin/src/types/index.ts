export type UserRole = "Admin" | "Driver" | "Parent";
export type AccountStatus = "Pending" | "Active" | "Suspended" | "Rejected";

export interface User {
  id: number;
  role: UserRole;
  fullName: string;
  email: string;
  phoneNumber: string;
  status: AccountStatus;
}

export interface Vehicle {
  id: number;
  registrationNumber: string;
  capacity: number;
  photoUrls: string[];
  approvalStatus: AccountStatus;
}

export interface Stop {
  id: number;
  sequence: number;
  school: string;
  pickupAddress: string;
  latitude: number;
  longitude: number;
  arrivalTime: string;
}

export interface Route {
  id: number;
  name: string;
  pricePerDay: number;
  pricePerMonth: number;
  stops: Stop[];
}

export interface Subscription {
  id: number;
  driverId: number;
  billingYear: number;
  billingMonth: number;
  amount: number;
  paidAt: string | null;
  status: "Pending" | "Completed" | "Overdue";
}

export interface Payment {
  id: number;
  applicationId: number;
  billingYear: number;
  billingMonth: number;
  amount: number;
  method: "Cash" | "Bank";
  status: "Pending" | "Completed";
  slipImageUrl: string | null;
  paidAt: string | null;
}

export interface DashboardSummary {
  activeDrivers: number;
  pendingDrivers: number;
  registeredParents: number;
  registeredStudents: number;
  totalVehicles: number;
  subscriptions: { year: number; month: number; paid: number; pending: number; overdue: number; revenue: number };
}

export interface DriverRow extends User {
  createdAt: string;
  vehicle: Vehicle | null;
}

export interface DriverDetail extends DriverRow {
  bank: { bankName: string | null; bankBranch: string | null; bankAccountNumber: string | null };
  routes: Route[];
  subscriptions: Subscription[];
  acceptedSeats: number;
}
