const state = {
  token: sessionStorage.getItem("gytt_admin_token") || "",
  user: JSON.parse(sessionStorage.getItem("gytt_admin_user") || "null")
};

const loginView = document.querySelector("#loginView");
const dashboardView = document.querySelector("#dashboardView");
const loginForm = document.querySelector("#loginForm");
const loginStatus = document.querySelector("#loginStatus");
const createForm = document.querySelector("#createForm");
const createStatus = document.querySelector("#createStatus");
const userRows = document.querySelector("#userRows");
const userCount = document.querySelector("#userCount");
const adminName = document.querySelector("#adminName");

function setStatus(element, message, kind = "") {
  element.textContent = message || "";
  element.className = `status ${kind}`.trim();
}

async function api(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  if (state.token) headers.Authorization = `Bearer ${state.token}`;
  const response = await fetch(path, { ...options, headers });
  const text = await response.text();
  let payload = null;
  try { payload = text ? JSON.parse(text) : null; } catch { payload = null; }
  if (!response.ok) {
    const validationMessage = payload?.errors
      ? Object.values(payload.errors).flat()[0]
      : "";
    const error = new Error(
      payload?.message || validationMessage || `请求失败（HTTP ${response.status}）`);
    error.status = response.status;
    throw error;
  }
  return payload;
}

function showLogin() {
  loginView.classList.remove("hidden");
  dashboardView.classList.add("hidden");
}

function showDashboard() {
  loginView.classList.add("hidden");
  dashboardView.classList.remove("hidden");
  adminName.textContent = `${state.user.username} · ${state.user.role}`;
  loadUsers();
}

async function loadUsers() {
  try {
    const data = await api("/api/admin/users?limit=500");
    userRows.replaceChildren();
    userCount.textContent = `共 ${data.total} 个账号`;
    for (const user of data.users) {
      const row = document.createElement("tr");
      const name = document.createElement("td");
      const roleCell = document.createElement("td");
      const date = document.createElement("td");
      const actions = document.createElement("td");
      const role = document.createElement("span");
      const reset = document.createElement("button");
      name.textContent = user.username;
      role.textContent = user.role;
      role.className = `role ${user.role}`;
      roleCell.append(role);
      date.textContent = new Date(user.createTime).toLocaleString();
      reset.type = "button";
      reset.className = "ghost reset-password";
      reset.textContent = "重置密码";
      reset.addEventListener("click", () => resetPassword(user.username));
      actions.append(reset);
      row.append(name, roleCell, date, actions);
      userRows.append(row);
    }
  } catch (error) {
    if (error.status === 401 || error.status === 403) {
      logout();
      setStatus(loginStatus, "管理员登录已过期，请重新登录。", "error");
      return;
    }
    setStatus(createStatus, error.message, "error");
  }
}

async function resetPassword(username) {
  const password = prompt(`为 ${username} 输入新密码（至少 6 位）`);
  if (password == null) return;
  if (password.length < 6) {
    setStatus(createStatus, "密码至少需要 6 位。", "error");
    return;
  }
  try {
    await api(`/api/admin/users/${encodeURIComponent(username)}/password`, {
      method: "PUT",
      body: JSON.stringify({ password })
    });
    setStatus(createStatus, `账号 ${username} 的密码已更新。`, "success");
  } catch (error) {
    setStatus(createStatus, error.message, "error");
  }
}

function logout() {
  state.token = "";
  state.user = null;
  sessionStorage.removeItem("gytt_admin_token");
  sessionStorage.removeItem("gytt_admin_user");
  showLogin();
}

loginForm.addEventListener("submit", async event => {
  event.preventDefault();
  const button = loginForm.querySelector("button");
  button.disabled = true;
  setStatus(loginStatus, "正在验证管理员身份…");
  try {
    const data = await api("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({
        username: document.querySelector("#loginUsername").value.trim(),
        password: document.querySelector("#loginPassword").value
      })
    });
    if (data.user.role !== "admin") {
      throw new Error("当前账号不是管理员，无法进入后台。");
    }
    state.token = data.token;
    state.user = data.user;
    sessionStorage.setItem("gytt_admin_token", data.token);
    sessionStorage.setItem("gytt_admin_user", JSON.stringify(data.user));
    setStatus(loginStatus, "");
    showDashboard();
  } catch (error) {
    setStatus(loginStatus, error.message, "error");
  } finally {
    button.disabled = false;
  }
});

createForm.addEventListener("submit", async event => {
  event.preventDefault();
  const button = createForm.querySelector("button");
  button.disabled = true;
  setStatus(createStatus, "正在创建账号…");
  try {
    const username = document.querySelector("#newUsername").value.trim();
    const password = document.querySelector("#newPassword").value;
    const role = document.querySelector("#newRole").value;
    await api("/api/admin/users", {
      method: "POST",
      body: JSON.stringify({ username, password, role })
    });
    setStatus(createStatus, `账号 ${username} 创建成功。`, "success");
    createForm.reset();
    await loadUsers();
  } catch (error) {
    setStatus(createStatus, error.message, "error");
  } finally {
    button.disabled = false;
  }
});

document.querySelector("#refreshButton").addEventListener("click", loadUsers);
document.querySelector("#logoutButton").addEventListener("click", logout);

if (state.token && state.user?.role === "admin") {
  showDashboard();
} else {
  showLogin();
}
