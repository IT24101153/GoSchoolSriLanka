import { useState } from "react";
import { Link } from "react-router-dom";
import { apiClient } from "../api/client";
import { act, useFetch } from "../api/useFetch";
import type { AccountStatus, DriverRow } from "../types";

export function DriversPage() {
  const [status, setStatus] = useState<AccountStatus | "">("Pending");
  const { data, error, reload } = useFetch<DriverRow[]>(`/admin/drivers${status ? `?status=${status}` : ""}`);

  const post = (path: string) => act(() => apiClient.post(path), reload);

  return (
    <section>
      <h1>Drivers</h1>
      <label>
        Status
        <select id="status" value={status} onChange={(e) => setStatus(e.target.value as AccountStatus | "")}>
          <option value="">All</option>
          <option>Pending</option>
          <option>Active</option>
          <option>Suspended</option>
          <option>Rejected</option>
        </select>
      </label>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Phone</th>
            <th>Vehicle</th>
            <th>Capacity</th>
            <th>Photos</th>
            <th>Status</th>
            <th>Registered</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {data?.map((d) => (
            <tr key={d.id}>
              <td>
                <Link to={`/drivers/${d.id}`}>{d.fullName}</Link>
              </td>
              <td>{d.email}</td>
              <td>{d.phoneNumber}</td>
              <td>{d.vehicle?.registrationNumber ?? "—"}</td>
              <td>{d.vehicle?.capacity ?? "—"}</td>
              <td>
                {d.vehicle?.photoUrls.map((url, i) => (
                  <a key={url} href={url} target="_blank" rel="noreferrer">
                    photo {i + 1}{" "}
                  </a>
                ))}
              </td>
              <td>{d.status}</td>
              <td>{new Date(d.createdAt).toLocaleDateString()}</td>
              <td>
                {d.status === "Pending" && (
                  <>
                    <button onClick={() => post(`/admin/drivers/${d.id}/approve`)}>Approve</button>
                    <button onClick={() => post(`/admin/drivers/${d.id}/reject`)}>Reject</button>
                  </>
                )}
                {d.status === "Active" && <button onClick={() => post(`/admin/users/${d.id}/suspend`)}>Suspend</button>}
                {d.status === "Suspended" && <button onClick={() => post(`/admin/users/${d.id}/reactivate`)}>Reactivate</button>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {data?.length === 0 && <p>No drivers.</p>}
    </section>
  );
}
