import { useState } from "react";
import { apiClient } from "../api/client";
import { act, useFetch } from "../api/useFetch";
import type { User, UserRole } from "../types";

export function UsersPage() {
  const [role, setRole] = useState<UserRole | "">("");
  const { data, error, reload } = useFetch<User[]>(`/admin/users${role ? `?role=${role}` : ""}`);

  return (
    <section>
      <h1>Access Management</h1>
      <label>
        Role
        <select id="role" value={role} onChange={(e) => setRole(e.target.value as UserRole | "")}>
          <option value="">All</option>
          <option>Driver</option>
          <option>Parent</option>
        </select>
      </label>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Role</th>
            <th>Email</th>
            <th>Phone</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data?.map((u) => (
            <tr key={u.id}>
              <td>{u.fullName}</td>
              <td>{u.role}</td>
              <td>{u.email}</td>
              <td>{u.phoneNumber}</td>
              <td>{u.status}</td>
              <td>
                {u.status === "Active" && <button onClick={() => act(() => apiClient.post(`/admin/users/${u.id}/suspend`), reload)}>Suspend</button>}
                {u.status === "Suspended" && (
                  <button onClick={() => act(() => apiClient.post(`/admin/users/${u.id}/reactivate`), reload)}>Reactivate</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
