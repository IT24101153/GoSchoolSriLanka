import { useParams } from "react-router-dom";
import { apiClient } from "../api/client";
import { act, useFetch } from "../api/useFetch";
import type { DriverDetail } from "../types";

export function DriverDetailPage() {
  const { id } = useParams();
  const { data: d, error, reload } = useFetch<DriverDetail>(`/admin/drivers/${id}`);
  if (error) return <p role="alert">{error}</p>;
  if (!d) return <p>Loading…</p>;

  return (
    <section>
      <h1>{d.fullName}</h1>
      <p>
        {d.email} · {d.phoneNumber} · {d.status} · seats taken: {d.acceptedSeats}/{d.vehicle?.capacity ?? "?"}
      </p>

      <h2>Vehicle</h2>
      {d.vehicle ? (
        <p>
          {d.vehicle.registrationNumber}, capacity {d.vehicle.capacity}, {d.vehicle.approvalStatus}.{" "}
          {d.vehicle.photoUrls.map((url, i) => (
            <a key={url} href={url} target="_blank" rel="noreferrer">
              photo {i + 1}{" "}
            </a>
          ))}
        </p>
      ) : (
        <p>No vehicle.</p>
      )}

      <h2>Bank details</h2>
      <p>
        {d.bank.bankName ?? "—"} / {d.bank.bankBranch ?? "—"} / {d.bank.bankAccountNumber ?? "—"}
      </p>

      <h2>Routes</h2>
      {d.routes.length === 0 && <p>No routes yet.</p>}
      {d.routes.map((r) => (
        <div key={r.id}>
          <h3>
            {r.name} — LKR {r.pricePerDay}/day, LKR {r.pricePerMonth}/month
          </h3>
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>School</th>
                <th>Pickup</th>
                <th>Arrival</th>
              </tr>
            </thead>
            <tbody>
              {r.stops.map((s) => (
                <tr key={s.id}>
                  <td>{s.sequence}</td>
                  <td>{s.school}</td>
                  <td>{s.pickupAddress}</td>
                  <td>{s.arrivalTime.slice(0, 5)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}

      <h2>Subscriptions</h2>
      <table>
        <thead>
          <tr>
            <th>Period</th>
            <th>Amount</th>
            <th>Status</th>
            <th>Paid at</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {d.subscriptions.map((s) => (
            <tr key={s.id}>
              <td>
                {s.billingYear}-{String(s.billingMonth).padStart(2, "0")}
              </td>
              <td>LKR {s.amount}</td>
              <td>{s.status}</td>
              <td>{s.paidAt ? new Date(s.paidAt).toLocaleString() : "—"}</td>
              <td>
                {s.status !== "Completed" && (
                  <button onClick={() => act(() => apiClient.post(`/admin/subscriptions/${s.id}/mark-paid`), reload)}>Mark paid</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
