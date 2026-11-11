import { useFetch } from "../api/useFetch";
import type { DashboardSummary } from "../types";

export function DashboardPage() {
  const { data, error } = useFetch<DashboardSummary>("/admin/dashboard");
  if (error) return <p role="alert">{error}</p>;
  if (!data) return <p>Loading…</p>;

  const s = data.subscriptions;
  return (
    <section>
      <h1>Master Dashboard</h1>
      <ul>
        <li>Active drivers: {data.activeDrivers}</li>
        <li>Pending driver approvals: {data.pendingDrivers}</li>
        <li>Registered parents: {data.registeredParents}</li>
        <li>Registered students: {data.registeredStudents}</li>
        <li>Total vehicles: {data.totalVehicles}</li>
      </ul>
      <h2>
        Driver subscriptions — {s.year}-{String(s.month).padStart(2, "0")}
      </h2>
      <ul>
        <li>Paid: {s.paid}</li>
        <li>Pending: {s.pending}</li>
        <li>Overdue (all months): {s.overdue}</li>
        <li>Revenue this month: LKR {s.revenue}</li>
      </ul>
    </section>
  );
}
