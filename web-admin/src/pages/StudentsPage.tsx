import { useFetch } from "../api/useFetch";
import type { User } from "../types";

interface StudentRow {
  id: number;
  name: string;
  school: string;
  schoolAddress: string;
  primaryContactNumber: string;
  emergencyContactNumber: string;
  parent: User;
  assignment: { routeId: number; route: string; driver: string; stop: string; arrivalTime: string } | null;
}

export function StudentsPage() {
  const { data, error } = useFetch<StudentRow[]>("/admin/students");
  return (
    <section>
      <h1>Students</h1>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>School</th>
            <th>Parent</th>
            <th>Contacts</th>
            <th>Driver / route</th>
            <th>Pickup</th>
          </tr>
        </thead>
        <tbody>
          {data?.map((s) => (
            <tr key={s.id}>
              <td>{s.name}</td>
              <td>
                {s.school} — {s.schoolAddress}
              </td>
              <td>
                {s.parent.fullName} ({s.parent.phoneNumber})
              </td>
              <td>
                {s.primaryContactNumber} / {s.emergencyContactNumber}
              </td>
              <td>{s.assignment ? `${s.assignment.driver} — ${s.assignment.route}` : "Unassigned"}</td>
              <td>{s.assignment ? `${s.assignment.stop} at ${s.assignment.arrivalTime.slice(0, 5)}` : "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
