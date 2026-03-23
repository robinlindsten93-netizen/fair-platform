import { useMemo, useState } from "react";

const API_BASE = "http://localhost:5001";

const panelStyle = {
  border: "1px solid #d0d7de",
  borderRadius: 12,
  padding: 20,
  background: "#ffffff",
  boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
  overflow: "auto",
  minHeight: 0
};

const inputStyle = {
  width: "100%",
  padding: "10px 12px",
  borderRadius: 8,
  border: "1px solid #cbd5e1",
  fontSize: 14,
  boxSizing: "border-box"
};

const buttonRowStyle = {
  display: "flex",
  gap: 10,
  flexWrap: "wrap",
  marginTop: 12
};

const preStyle = {
  background: "#0f172a",
  color: "#e2e8f0",
  padding: 12,
  borderRadius: 8,
  overflow: "auto",
  whiteSpace: "pre-wrap",
  wordBreak: "break-word",
  fontSize: 13,
  lineHeight: 1.4,
  maxHeight: 180
};

const badgeStyle = (active) => ({
  display: "inline-block",
  padding: "4px 10px",
  borderRadius: 999,
  fontSize: 12,
  fontWeight: 700,
  background: active ? "#dcfce7" : "#e2e8f0",
  color: active ? "#166534" : "#475569",
  marginRight: 8,
  marginBottom: 8
});

const statusBoxStyle = {
  background: "#f8fafc",
  border: "1px solid #e2e8f0",
  borderRadius: 10,
  padding: 12,
  marginTop: 16,
  fontSize: 14,
  lineHeight: 1.5
};

const hintStyle = (ok) => ({
  color: ok ? "#166534" : "#b45309",
  fontWeight: 600
});

export default function App() {
  // RIDER
  const [phone, setPhone] = useState("+46700000001");
  const [code, setCode] = useState("123456");
  const [token, setToken] = useState("");
  const [me, setMe] = useState(null);

  // DRIVER
  const [driverPhone, setDriverPhone] = useState("+46700000002");
  const [driverCode, setDriverCode] = useState("123456");
  const [driverToken, setDriverToken] = useState("");
  const [driverMe, setDriverMe] = useState(null);

  // SHARED
  const [result, setResult] = useState("Inte testat ännu");
  const [loading, setLoading] = useState(false);

  // RIDER TRIP DATA
  const [pickupLat, setPickupLat] = useState("59.3293");
  const [pickupLng, setPickupLng] = useState("18.0686");
  const [dropoffLat, setDropoffLat] = useState("59.3493");
  const [dropoffLng, setDropoffLng] = useState("18.0986");
  const [quoteToken, setQuoteToken] = useState("");
  const [tripId, setTripId] = useState("");
  const [activeTrip, setActiveTrip] = useState(null);

  // DRIVER DATA
  const [driverLat, setDriverLat] = useState("59.3293");
  const [driverLng, setDriverLng] = useState("18.0686");
  const [offers, setOffers] = useState([]);
  const [selectedOfferId, setSelectedOfferId] = useState("");

  const riderHasRole = useMemo(
    () => Array.isArray(me?.roles) && me.roles.includes("RIDER"),
    [me]
  );

  const driverHasRole = useMemo(
    () => Array.isArray(driverMe?.roles) && driverMe.roles.includes("DRIVER"),
    [driverMe]
  );

  async function readText(res) {
    return await res.text();
  }

  async function readJsonSafe(res) {
    const text = await res.text();
    if (!text) return { text: "", data: null };

    try {
      return { text, data: JSON.parse(text) };
    } catch {
      return { text, data: null };
    }
  }

  function resetTripFlow() {
    setQuoteToken("");
    setTripId("");
    setActiveTrip(null);
  }

  // =========================
  // SESSION REFRESH HELPERS
  // =========================
  async function refreshRiderSession() {
    const verifyRes = await fetch(`${API_BASE}/api/v1/auth/otp/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ phone, code })
    });

    const { data: verifyData } = await readJsonSafe(verifyRes);
    const newToken = verifyData?.access_token ?? "";

    if (!verifyRes.ok || !newToken) {
      throw new Error("Rider OTP verify misslyckades vid session refresh.");
    }

    setToken(newToken);

    const meRes = await fetch(`${API_BASE}/api/v1/me`, {
      headers: { Authorization: `Bearer ${newToken}` }
    });

    const { data: meData, text: meText } = await readJsonSafe(meRes);
    if (!meRes.ok || !meData) {
      throw new Error(`Rider /me misslyckades vid session refresh.\n${meText}`);
    }

    setMe(meData);
    return { token: newToken, me: meData };
  }

  async function refreshDriverSession() {
    const verifyRes = await fetch(`${API_BASE}/api/v1/auth/otp/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ phone: driverPhone, code: driverCode })
    });

    const { data: verifyData } = await readJsonSafe(verifyRes);
    const newToken = verifyData?.access_token ?? "";

    if (!verifyRes.ok || !newToken) {
      throw new Error("Driver OTP verify misslyckades vid session refresh.");
    }

    setDriverToken(newToken);

    const meRes = await fetch(`${API_BASE}/api/v1/me`, {
      headers: { Authorization: `Bearer ${newToken}` }
    });

    const { data: meData, text: meText } = await readJsonSafe(meRes);
    if (!meRes.ok || !meData) {
      throw new Error(`Driver /me misslyckades vid session refresh.\n${meText}`);
    }

    setDriverMe(meData);
    return { token: newToken, me: meData };
  }

  // =========================
  // RIDER AUTH
  // =========================
  async function otpRequest() {
    setLoading(true);
    setResult("Skickar Rider OTP request...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/request`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone })
      });

      const text = await readText(res);
      setResult(`Rider OTP request status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function otpVerify() {
    setLoading(true);
    setResult("Verifierar Rider OTP...");

    try {
      const refreshed = await refreshRiderSession();
      setResult(`Rider OTP verify status: 200\n\nRider-token sparad.\n\nToken prefix: ${refreshed.token.slice(0, 40)}...`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function loadMe() {
    setLoading(true);
    setResult("Hämtar Rider /me ...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/me`, {
        headers: { Authorization: `Bearer ${token}` }
      });

      const { data, text } = await readJsonSafe(res);
      if (data) {
        setMe(data);
        setResult(`Rider GET /me status: ${res.status}\n\n${JSON.stringify(data, null, 2)}`);
      } else {
        setMe(null);
        setResult(`Rider GET /me status: ${res.status}\n\n${text}`);
      }
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function grantRiderRole() {
    if (!me?.userId) {
      setResult("Rider userId saknas. Kör Load /me först.");
      return;
    }

    setLoading(true);
    setResult("Grantar Rider-roll...");

    try {
      const grantRes = await fetch(`${API_BASE}/api/v1/dev/roles/grant`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          userId: me.userId,
          role: "Rider"
        })
      });

      const grantText = await readText(grantRes);

      if (!grantRes.ok) {
        setResult(`Grant Rider status: ${grantRes.status}\n\n${grantText}`);
        return;
      }

      const refreshed = await refreshRiderSession();

      setResult(
        `Grant Rider status: ${grantRes.status}\n\n${grantText}\n\n` +
        `Rider session refresh OK\n\n` +
        `Rider /me efter refresh:\n${JSON.stringify(refreshed.me, null, 2)}`
      );
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // RIDER TRIP FLOW
  // =========================
  async function createQuote() {
    setLoading(true);
    setResult("Creating quote...");
    resetTripFlow();

    try {
      const refreshed = await refreshRiderSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("RIDER")) {
        setResult(`Rider saknar RIDER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/trips/quotes`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${refreshed.token}`
        },
        body: JSON.stringify({
          pickupLat: Number(pickupLat),
          pickupLng: Number(pickupLng),
          dropoffLat: Number(dropoffLat),
          dropoffLng: Number(dropoffLng),
          mode: 0
        })
      });

      const { data, text } = await readJsonSafe(res);

      if (!res.ok) {
        setResult(`Create quote failed\nStatus: ${res.status}\n\n${text}`);
        return;
      }

      setQuoteToken(data?.quoteToken ?? "");
      setResult(`Quote OK\n\n${JSON.stringify(data, null, 2)}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function createTrip() {
    setLoading(true);
    setResult("Creating trip...");

    try {
      if (!quoteToken) {
        setResult("QuoteToken saknas. Kör Create Quote först.");
        return;
      }

      const refreshed = await refreshRiderSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("RIDER")) {
        setResult(`Rider saknar RIDER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/trips`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${refreshed.token}`
        },
        body: JSON.stringify({
          pickupLat: Number(pickupLat),
          pickupLng: Number(pickupLng),
          dropoffLat: Number(dropoffLat),
          dropoffLng: Number(dropoffLng),
          mode: 0,
          quoteToken
        })
      });

      const { data, text } = await readJsonSafe(res);

      if (!res.ok) {
        setResult(
          `Create trip failed\nStatus: ${res.status}\n\n${text}\n\n` +
          `Rider /me just nu:\n${JSON.stringify(refreshed.me, null, 2)}`
        );
        return;
      }

      const createdTripId = data?.tripId ?? data?.id ?? "";
      setTripId(createdTripId);

      setResult(`Trip created\nStatus: ${res.status}\n\n${text || "Tomt svar från backend"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function requestTrip() {
    setLoading(true);
    setResult("Requesting trip...");

    try {
      if (!tripId) {
        setResult("TripId saknas. Kör Create Trip först.");
        return;
      }

      const refreshed = await refreshRiderSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("RIDER")) {
        setResult(`Rider saknar RIDER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/trips/${tripId}/request`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${refreshed.token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          quoteToken
        })
      });

      const text = await readText(res);

      if (!res.ok) {
        setResult(`Request trip failed\nStatus: ${res.status}\n\n${text}`);
        return;
      }

      setActiveTrip({
        tripId,
        status: "Requested"
      });

      setResult(`Trip requested\nStatus: ${res.status}\n\n${text || "Tomt svar från backend"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function getActiveTrip() {
    setLoading(true);
    setResult("Hämtar aktiv trip...");

    try {
      const refreshed = await refreshRiderSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("RIDER")) {
        setResult(`Rider saknar RIDER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/trips/active`, {
        headers: {
          Authorization: `Bearer ${refreshed.token}`
        }
      });

      const { data, text } = await readJsonSafe(res);

      if (res.status === 404) {
        setActiveTrip(null);
        setResult("Ingen aktiv trip för ridern just nu.");
        return;
      }

      if (!res.ok) {
        setResult(`Get active trip failed\nStatus: ${res.status}\n\n${text}`);
        return;
      }

      setActiveTrip(data);
      setResult(`Active trip status: ${res.status}\n\n${JSON.stringify(data, null, 2)}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // DRIVER AUTH
  // =========================
  async function driverOtpRequest() {
    setLoading(true);
    setResult("Skickar Driver OTP request...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/request`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone: driverPhone })
      });

      const text = await readText(res);
      setResult(`Driver OTP request status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function driverOtpVerify() {
    setLoading(true);
    setResult("Verifierar Driver OTP...");

    try {
      const refreshed = await refreshDriverSession();
      setResult(
        `Driver OTP verify status: 200\n\n` +
        `Driver-token sparad.\n\nToken prefix: ${refreshed.token.slice(0, 40)}...`
      );
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function loadDriverMe() {
    setLoading(true);
    setResult("Hämtar Driver /me ...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/me`, {
        headers: { Authorization: `Bearer ${driverToken}` }
      });

      const { data, text } = await readJsonSafe(res);

      if (data) {
        setDriverMe(data);
        setResult(`Driver GET /me status: ${res.status}\n\n${JSON.stringify(data, null, 2)}`);
      } else {
        setDriverMe(null);
        setResult(`Driver GET /me status: ${res.status}\n\n${text}`);
      }
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function grantDriverRole() {
    if (!driverMe?.userId) {
      setResult("Driver userId saknas. Kör Driver Load /me först.");
      return;
    }

    setLoading(true);
    setResult("Grantar Driver-roll...");

    try {
      const grantRes = await fetch(`${API_BASE}/api/v1/dev/roles/grant`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${driverToken}`
        },
        body: JSON.stringify({
          userId: driverMe.userId,
          role: "Driver"
        })
      });

      const grantText = await readText(grantRes);

      if (!grantRes.ok) {
        setResult(`Grant Driver status: ${grantRes.status}\n\n${grantText}`);
        return;
      }

      const refreshed = await refreshDriverSession();

      setResult(
        `Grant Driver status: ${grantRes.status}\n\n${grantText}\n\n` +
        `Driver session refresh OK\n\n` +
        `Driver /me efter refresh:\n${JSON.stringify(refreshed.me, null, 2)}`
      );
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // DRIVER ACTIONS
  // =========================
  async function setDriverOnline() {
    setLoading(true);
    setResult("Sätter driver online...");

    try {
      const refreshed = await refreshDriverSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("DRIVER")) {
        setResult(`Driver saknar DRIVER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/driver/me/availability`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${refreshed.token}`
        },
        body: JSON.stringify({
          isOnline: true
        })
      });

      const text = await readText(res);
      setResult(`Driver online status: ${res.status}\n\n${text || "OK"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function sendDriverLocation() {
    setLoading(true);
    setResult("Skickar driver location...");

    try {
      const refreshed = await refreshDriverSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("DRIVER")) {
        setResult(`Driver saknar DRIVER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/driver/location`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${refreshed.token}`
        },
        body: JSON.stringify({
          lat: Number(driverLat),
          lng: Number(driverLng)
        })
      });

      const text = await readText(res);
      setResult(`Driver location status: ${res.status}\n\n${text || "OK"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function getOffers() {
    setLoading(true);
    setResult("Hämtar driver offers...");

    try {
      const refreshed = await refreshDriverSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("DRIVER")) {
        setResult(`Driver saknar DRIVER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/dispatch/offers`, {
        headers: {
          Authorization: `Bearer ${refreshed.token}`
        }
      });

      const { data, text } = await readJsonSafe(res);
      const list = Array.isArray(data) ? data : [];

      setOffers(list);

      if (list.length > 0) {
        setSelectedOfferId(list[0].offerId ?? list[0].id ?? "");
      }

      setResult(`Get offers status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function acceptOffer() {
    setLoading(true);
    setResult("Accepterar offer...");

    try {
      if (!selectedOfferId) {
        setResult("OfferId saknas. Kör Get Offers först.");
        return;
      }

      const refreshed = await refreshDriverSession();

      if (!Array.isArray(refreshed.me.roles) || !refreshed.me.roles.includes("DRIVER")) {
        setResult(`Driver saknar DRIVER-roll efter refresh.\n\n${JSON.stringify(refreshed.me, null, 2)}`);
        return;
      }

      const res = await fetch(`${API_BASE}/api/v1/dispatch/offers/accept`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${refreshed.token}`
        },
        body: JSON.stringify({
          offerId: selectedOfferId
        })
      });

      const text = await readText(res);
      setResult(`Accept offer status: ${res.status}\n\n${text || "OK"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#f8fafc",
        color: "#0f172a",
        padding: 24,
        boxSizing: "border-box"
      }}
    >
      <div
        style={{
          maxWidth: 1400,
          margin: "0 auto",
          display: "grid",
          gridTemplateRows: "auto 1fr auto",
          gap: 20,
          minHeight: "calc(100vh - 48px)"
        }}
      >
        <header>
          <h1 style={{ margin: 0, fontSize: 40 }}>Fair Frontend 🚀</h1>
          <p style={{ marginTop: 8, color: "#475569" }}>
            Rider + Driver testpanel för backend och dispatchflöden.
          </p>
        </header>

        <main
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: 20,
            minHeight: 0
          }}
        >
          <section style={panelStyle}>
            <h2 style={{ marginTop: 0 }}>Rider</h2>

            <div style={{ marginBottom: 10 }}>
              <span style={badgeStyle(riderHasRole)}>RIDER</span>
            </div>

            <div style={statusBoxStyle}>
              <div><strong>Rider status</strong></div>
              <div style={hintStyle(!!token)}>Token: {token ? "OK" : "Saknas"}</div>
              <div style={hintStyle(riderHasRole)}>Role: {riderHasRole ? "RIDER aktiv" : "RIDER saknas i aktiv session"}</div>
              <div style={hintStyle(!!quoteToken)}>Quote: {quoteToken ? "Finns" : "Saknas"}</div>
              <div style={hintStyle(!!tripId)}>Trip: {tripId ? "Finns" : "Saknas"}</div>
              <div style={hintStyle(!!activeTrip)}>Active Trip: {activeTrip ? "Finns" : "Ingen laddad"}</div>
            </div>

            <div style={{ display: "grid", gap: 10 }}>
              <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Rider Telefon" style={inputStyle} />
              <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="Rider OTP" style={inputStyle} />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={otpRequest} disabled={loading}>OTP request</button>
              <button onClick={otpVerify} disabled={loading}>OTP verify</button>
              <button onClick={loadMe} disabled={!token || loading}>Load /me</button>
              <button onClick={grantRiderRole} disabled={!me?.userId || loading}>Grant Rider</button>
              <button onClick={getActiveTrip} disabled={!token || loading}>Get Active Trip</button>
            </div>

            <h3 style={{ marginTop: 24 }}>Trip</h3>

            <div style={{ display: "grid", gap: 10 }}>
              <input value={pickupLat} onChange={(e) => setPickupLat(e.target.value)} placeholder="Pickup Lat" style={inputStyle} />
              <input value={pickupLng} onChange={(e) => setPickupLng(e.target.value)} placeholder="Pickup Lng" style={inputStyle} />
              <input value={dropoffLat} onChange={(e) => setDropoffLat(e.target.value)} placeholder="Dropoff Lat" style={inputStyle} />
              <input value={dropoffLng} onChange={(e) => setDropoffLng(e.target.value)} placeholder="Dropoff Lng" style={inputStyle} />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={createQuote} disabled={!token || loading}>Create Quote</button>
              <button onClick={createTrip} disabled={!quoteToken || loading}>Create Trip</button>
              <button onClick={requestTrip} disabled={!tripId || loading}>Request Trip</button>
            </div>

            <h4 style={{ marginTop: 24 }}>Rider /me</h4>
            <pre style={preStyle}>
              {me ? JSON.stringify(me, null, 2) : "Ingen rider /me-data ännu"}
            </pre>

            <h4>QuoteToken</h4>
            <pre style={preStyle}>{quoteToken || "Ingen quote ännu"}</pre>

            <h4>TripId</h4>
            <pre style={preStyle}>{tripId || "Ingen trip ännu"}</pre>

            <h4>Active Trip</h4>
            <pre style={preStyle}>
              {activeTrip ? JSON.stringify(activeTrip, null, 2) : "Ingen aktiv trip laddad"}
            </pre>
          </section>

          <section style={panelStyle}>
            <h2 style={{ marginTop: 0 }}>Driver</h2>

            <div style={{ marginBottom: 10 }}>
              <span style={badgeStyle(driverHasRole)}>DRIVER</span>
            </div>

            <div style={statusBoxStyle}>
              <div><strong>Driver status</strong></div>
              <div style={hintStyle(!!driverToken)}>Token: {driverToken ? "OK" : "Saknas"}</div>
              <div style={hintStyle(driverHasRole)}>Role: {driverHasRole ? "DRIVER aktiv" : "DRIVER saknas i aktiv session"}</div>
              <div style={hintStyle(offers.length > 0)}>Offers: {offers.length > 0 ? `${offers.length} st` : "Inga"}</div>
              <div style={hintStyle(!!selectedOfferId)}>Selected offer: {selectedOfferId ? "Vald" : "Ingen vald"}</div>
            </div>

            <div style={{ display: "grid", gap: 10 }}>
              <input value={driverPhone} onChange={(e) => setDriverPhone(e.target.value)} placeholder="Driver Telefon" style={inputStyle} />
              <input value={driverCode} onChange={(e) => setDriverCode(e.target.value)} placeholder="Driver OTP" style={inputStyle} />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={driverOtpRequest} disabled={loading}>OTP request</button>
              <button onClick={driverOtpVerify} disabled={loading}>OTP verify</button>
              <button onClick={loadDriverMe} disabled={!driverToken || loading}>Load /me</button>
              <button onClick={grantDriverRole} disabled={!driverMe?.userId || loading}>Grant Driver</button>
            </div>

            <h3 style={{ marginTop: 24 }}>Driver actions</h3>

            <div style={{ display: "grid", gap: 10 }}>
              <input value={driverLat} onChange={(e) => setDriverLat(e.target.value)} placeholder="Driver Lat" style={inputStyle} />
              <input value={driverLng} onChange={(e) => setDriverLng(e.target.value)} placeholder="Driver Lng" style={inputStyle} />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={setDriverOnline} disabled={!driverToken || loading}>Set Online</button>
              <button onClick={sendDriverLocation} disabled={!driverToken || loading}>Send Location</button>
              <button onClick={getOffers} disabled={!driverToken || loading}>Get Offers</button>
            </div>

            <h4 style={{ marginTop: 24 }}>Offers</h4>
            <pre style={preStyle}>
              {offers.length > 0 ? JSON.stringify(offers, null, 2) : "Inga offers ännu"}
            </pre>

            <input
              value={selectedOfferId}
              onChange={(e) => setSelectedOfferId(e.target.value)}
              placeholder="OfferId"
              style={inputStyle}
            />

            <div style={buttonRowStyle}>
              <button onClick={acceptOffer} disabled={!selectedOfferId || !driverToken || loading}>
                Accept Offer
              </button>
            </div>

            <h4 style={{ marginTop: 24 }}>Driver /me</h4>
            <pre style={preStyle}>
              {driverMe ? JSON.stringify(driverMe, null, 2) : "Ingen driver /me-data ännu"}
            </pre>
          </section>
        </main>

        <footer>
          <h2 style={{ marginBottom: 10 }}>Result</h2>
          <pre
            style={{
              ...preStyle,
              maxHeight: 220,
              margin: 0
            }}
          >
            {result}
          </pre>
        </footer>
      </div>
    </div>
  );
}