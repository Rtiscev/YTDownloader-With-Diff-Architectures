export async function isUserAuthenticated() {
    const token = localStorage.getItem("accessToken");
    if (!token) return false; // No token means not authenticated

    try {
        const res = await fetch("/api/auth/me", {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`
            }
        });
        return res.ok; // true if status 200, false otherwise
    } catch {
        return false;
    }
}
