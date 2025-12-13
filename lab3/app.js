const API_BASE_URL = "http://localhost:8080/api/v2";

let authToken = localStorage.getItem("authToken");
let currentLogin = localStorage.getItem("login");

const idempotencyCache = {
  register: null,
  createCourse: null
};

let teachersPage = 1;
let teachersPageSize = 10;

let coursesPage = 1;
let coursesPageSize = 10;

let teachersLookup = {};

const $ = (sel) => document.querySelector(sel);

// =======================
// УВЕДОМЛЕНИЯ
// =======================

function showError(message) {
  const box = $("#error-box");
  const textEl = $("#error-text");
  if (!box || !textEl) return;

  textEl.textContent = message;
  box.classList.remove("hidden");
  box.onclick = () => box.classList.add("hidden");
}

function hideError() {
  $("#error-box")?.classList.add("hidden");
}

function showSuccess(message) {
  const box = $("#success-box");
  const textEl = $("#success-text");
  if (!box || !textEl) return;

  textEl.textContent = message;
  box.classList.remove("hidden");
  box.onclick = () => box.classList.add("hidden");
}

function hideSuccess() {
  $("#success-box")?.classList.add("hidden");
}

// =======================
// АВТОРИЗАЦИЯ / ТОКЕН
// =======================

function setAuthToken(token, login) {
  authToken = token;
  currentLogin = login || null;

  token ? localStorage.setItem("authToken", token) : localStorage.removeItem("authToken");
  login ? localStorage.setItem("login", login) : localStorage.removeItem("login");
}

function updateUserUI() {
  const pill = $("#user-pill");
  const loginEl = $("#user-login");
  const avatarEl = $("#user-avatar");

  if (!pill || !loginEl || !avatarEl) return;

  if (authToken && currentLogin) {
    loginEl.textContent = currentLogin;
    avatarEl.textContent = currentLogin[0]?.toUpperCase() || "?";
    pill.style.display = "flex";
  } else {
    pill.style.display = "none";
  }
}

function requireAuth() {
  if (!authToken) {
    showError("Необходима авторизация.");
    window.location.href = "login.html";
    throw new Error("Not authenticated");
  }
}

// =======================
// ОБЩИЙ ЗАПРОС К API
// =======================

async function apiRequest(path, options = {}) {
  const {
    method = "GET",
    body = null,
    idempotent = false,
    idempotencyKeyName = null
  } = options;

  const headers = {
    "Content-Type": "application/json"
  };

  if (authToken) headers["Authorization"] = `Bearer ${authToken}`;

  if (idempotent) {
    let key = idempotencyCache[idempotencyKeyName];
    if (!key) {
      key = crypto.randomUUID();
      idempotencyCache[idempotencyKeyName] = key;
    }
    headers["IdempotencyKey"] = key;
  }

  let resp;
  try {
    resp = await fetch(API_BASE_URL + path, {
      method,
      headers,
      body: body ? JSON.stringify(body) : null
    });
  } catch (err) {
    showError("Ошибка сети. Проверьте подключение к серверу.");
    throw err;
  }

  if (!resp.ok) {
    let payload = null;
    try { payload = await resp.json(); } catch {}

    let message;

    if (resp.status === 429) {
      const retryAfter = resp.headers.get("Retry-After");
      message = payload?.message || `Слишком много запросов. Попробуйте через ${retryAfter || 5} сек.`;
    } else if (resp.status === 401) {
      message = payload?.message || "Неавторизовано.";
    } else {
      message = payload?.message || `Ошибка (${resp.status})`;
    }

    showError(message);

    if (resp.status === 401) {
      setAuthToken(null, null);
      updateUserUI();
      window.location.href = "login.html";
    }

    throw new Error(message);
  }

  hideError();

  if (resp.status === 204) return null;

  try {
    return await resp.json();
  } catch {
    return null;
  }
}

// =======================
// LOGIN
// =======================

async function initLoginPage() {
  if (authToken) {
    window.location.href = "teachers.html";
    return;
  }

  const form = $("#login-form");
  if (!form) return;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const login = form.login.value.trim();
    const password = form.password.value.trim();

    if (!login || !password) {
      showError("Введите логин и пароль.");
      return;
    }

    try {
      const resp = await apiRequest("/auth/login", {
        method: "POST",
        body: { login, password }
      });

      setAuthToken(resp.access, login);
      updateUserUI();
      showSuccess("Успешный вход.");

      setTimeout(() => window.location.href = "teachers.html", 800);
    } catch {}
  });
}

// =======================
// REGISTER
// =======================

async function initRegisterPage() {
  if (authToken) {
    window.location.href = "teachers.html";
    return;
  }

  const form = $("#register-form");
  if (!form) return;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const payload = {
      login: form.login.value.trim(),
      password: form.password.value.trim(),
      lastName: form.lastName.value.trim(),
      firstName: form.firstName.value.trim(),
      middleName: form.middleName.value.trim()
    };

    if (!payload.login || !payload.password || !payload.lastName || !payload.firstName) {
      showError("Заполните обязательные поля.");
      return;
    }

    try {
      await apiRequest("/auth/register", {
        method: "POST",
        body: payload,
        idempotent: true,
        idempotencyKeyName: "register"
      });

      idempotencyCache.register = null;
      showSuccess("Регистрация выполнена.");
      setTimeout(() => window.location.href = "login.html", 1000);
    } catch {}
  });
}

// =======================
// TEACHERS
// =======================

function openTeacherModal() {
  $("#teacher-modal").style.display = "flex";
}

function closeTeacherModal() {
  $("#teacher-modal").style.display = "none";
}

async function refreshTeachers() {
  const pageLabel = $("#teachers-page");
  if (!pageLabel) return;

  pageLabel.textContent = String(teachersPage);

  try {
    const teachers = await apiRequest(
      `/teachers?pageNumber=${teachersPage}&pageSize=${teachersPageSize}`
    );

    renderTeachersTable(teachers || []);

    $("#teachers-next").disabled = (teachers || []).length < teachersPageSize;
    $("#teachers-prev").disabled = teachersPage <= 1;
  } catch {}
}

function renderTeachersTable(teachers) {
  const tbody = $("#teachers-tbody");
  tbody.innerHTML = "";

  teachers.forEach(t => {
    const tr = document.createElement("tr");
    tr.dataset.id = t.id;
    tr.innerHTML = `
      <td>${t.login}</td>
      <td>${t.lastName}</td>
      <td>${t.firstName}</td>
      <td>${t.middleName || ""}</td>
      <td>
        <button class="btn btn-outline btn-sm" data-action="edit">✏️</button>
        <button class="btn btn-outline btn-sm" data-action="delete">🗑️</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

function fillTeacherFormFromRow(row) {
  const cells = row.querySelectorAll("td");
  const form = $("#teacher-form");
  form.id.value = row.dataset.id;
  form.login.value = cells[0].textContent.trim();
  form.lastName.value = cells[1].textContent.trim();
  form.firstName.value = cells[2].textContent.trim();
  form.middleName.value = cells[3].textContent.trim();
}

async function initTeachersPage() {
  requireAuth();
  updateUserUI();

  const form = $("#teacher-form");

  $("#logout-btn")?.addEventListener("click", () => {
    setAuthToken(null, null);
    updateUserUI();
    window.location.href = "login.html";
  });

  $("#teacher-modal").addEventListener("click", (e) => {
    if (e.target.id === "teacher-modal") {
      form.reset();
      closeTeacherModal();
    }
  });

  $("#teacher-modal-cancel")?.addEventListener("click", () => {
    form.reset();
    closeTeacherModal();
  });

  $("#teacher-modal-close")?.addEventListener("click", () => {
    form.reset();
    closeTeacherModal();
  });

  const sizeSelect = $("#teachers-page-size");
  if (sizeSelect) {
    sizeSelect.addEventListener("change", () => {
      teachersPageSize = Number(sizeSelect.value);
      teachersPage = 1;
      refreshTeachers();
    });
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const id = form.id.value;
    if (!id) {
      showError("Создание преподавателя выполняется на странице регистрации.");
      return;
    }

    const payload = {
      login: form.login.value.trim(),
      lastName: form.lastName.value.trim(),
      firstName: form.firstName.value.trim(),
      middleName: form.middleName.value.trim()
    };

    if (!payload.login || !payload.lastName || !payload.firstName) {
      showError("Заполните обязательные поля.");
      return;
    }

    try {
      await apiRequest(`/teachers/${id}`, {
        method: "PUT",
        body: payload
      });

      showSuccess("Обновлено.");
      form.reset();
      closeTeacherModal();
      teachersPage = 1;
      refreshTeachers();
    } catch {}
  });

  $("#teachers-tbody")?.addEventListener("click", async (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;

    const row = btn.closest("tr");
    const id = row.dataset.id;
    const action = btn.dataset.action;

    if (action === "edit") {
      fillTeacherFormFromRow(row);
      openTeacherModal();
    }

    if (action === "delete") {
      if (!confirm("Удалить преподавателя?")) return;

      try {
        await apiRequest(`/teachers/${id}`, { method: "DELETE" });
        showSuccess("Удалено.");
        teachersPage = 1;
        refreshTeachers();
      } catch {}
    }
  });

  $("#teachers-prev").addEventListener("click", () => {
    if (teachersPage > 1) {
      teachersPage--;
      refreshTeachers();
    }
  });

  $("#teachers-next").addEventListener("click", () => {
    teachersPage++;
    refreshTeachers();
  });

  refreshTeachers();
}

// =======================
// COURSES
// =======================

function openCourseModal() {
  $("#course-edit-modal").style.display = "flex";
}

function closeCourseModal() {
  $("#course-edit-modal").style.display = "none";
}

async function loadTeachersForCourseSelect() {
  const select = $("#course-edit-teacher");
  if (!select) return;

  try {
    const teachers = await apiRequest("/teachers?pageNumber=1&pageSize=200");

    teachersLookup = {};
    select.innerHTML = "";

    teachers.forEach(t => {
      const fio = `${t.lastName} ${t.firstName}${t.middleName ? " " + t.middleName : ""}`;
      teachersLookup[t.id] = fio;

      const opt = document.createElement("option");
      opt.value = t.id;
      opt.textContent = fio;
      select.appendChild(opt);
    });
  } catch {}
}

function renderCoursesTable(courses) {
  const tbody = $("#courses-tbody");
  tbody.innerHTML = "";

  courses.forEach(c => {
    const tr = document.createElement("tr");
    tr.dataset.id = c.id;
    tr.dataset.teacherId = c.teacherId;

    tr.innerHTML = `
      <td>${c.title}</td>
      <td>${c.description}</td>
      <td>${teachersLookup[c.teacherId] || "Не указан"}</td>
      <td>${c.createdAt ? new Date(c.createdAt).toLocaleString() : ""}</td>
      <td>
        <button class="btn btn-outline btn-sm" data-action="edit">✏️</button>
        <button class="btn btn-outline btn-sm" data-action="delete">🗑️</button>
      </td>
    `;

    tbody.appendChild(tr);
  });
}

function fillCourseEditFormFromRow(row) {
  const form = $("#course-edit-form");
  form.id.value = row.dataset.id;
  form.title.value = row.children[0].textContent.trim();
  form.description.value = row.children[1].textContent.trim();
  form.teacherId.value = row.dataset.teacherId;
}

async function refreshCourses() {
  $("#courses-page").textContent = String(coursesPage);

  try {
    const courses = await apiRequest(
      `/courses?pageNumber=${coursesPage}&pageSize=${coursesPageSize}`
    );

    renderCoursesTable(courses || []);

    $("#courses-next").disabled = (courses || []).length < coursesPageSize;
    $("#courses-prev").disabled = coursesPage <= 1;

  } catch {}
}

async function initCoursesPage() {
  requireAuth();
  updateUserUI();

  await loadTeachersForCourseSelect();

const coursesSizeSelect = $("#courses-page-size");
if (coursesSizeSelect) {
    coursesSizeSelect.addEventListener("change", () => {
        coursesPageSize = Number(coursesSizeSelect.value);
        coursesPage = 1;
        refreshCourses();
    });
}

  const form = $("#course-edit-form");

  $("#logout-btn").addEventListener("click", () => {
    setAuthToken(null, null);
    updateUserUI();
    window.location.href = "login.html";
  });

  $("#course-create-btn").addEventListener("click", () => {
    form.reset();
    form.id.value = "";
    $("#course-modal-title").textContent = "Создание курса";
    openCourseModal();
  });

  $("#course-edit-modal").addEventListener("click", (e) => {
    if (e.target.id === "course-edit-modal") {
      form.reset();
      closeCourseModal();
    }
  });

  $("#course-edit-close").addEventListener("click", () => {
    form.reset();
    closeCourseModal();
  });

  $("#course-edit-cancel").addEventListener("click", () => {
    form.reset();
    closeCourseModal();
  });

  // === PAGE SIZE SELECTOR FOR COURSES (если захочешь добавить позже)

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const id = form.id.value;
    const payload = {
      title: form.title.value.trim(),
      description: form.description.value.trim(),
      teacherId: form.teacherId.value
    };

    if (!payload.title || !payload.description) {
      showError("Заполните поля.");
      return;
    }

    try {
      if (id) {
        await apiRequest(`/courses/${id}`, {
          method: "PUT",
          body: payload
        });
        showSuccess("Курс обновлён.");
      } else {
        await apiRequest("/courses", {
          method: "POST",
          body: payload,
          idempotent: true,
          idempotencyKeyName: "createCourse"
        });
        idempotencyCache.createCourse = null;
        showSuccess("Курс создан.");
      }

      form.reset();
      closeCourseModal();
      coursesPage = 1;
      await loadTeachersForCourseSelect();
      refreshCourses();

    } catch {}
  });

  $("#courses-tbody").addEventListener("click", async (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;

    const row = btn.closest("tr");
    const id = row.dataset.id;
    const action = btn.dataset.action;

    if (action === "edit") {
      fillCourseEditFormFromRow(row);
      $("#course-modal-title").textContent = "Редактирование курса";
      openCourseModal();
    }

    if (action === "delete") {
      if (!confirm("Удалить курс?")) return;

      try {
        await apiRequest(`/courses/${id}`, { method: "DELETE" });
        showSuccess("Удалено.");
        coursesPage = 1;
        refreshCourses();
      } catch {}
    }
  });

  $("#courses-prev").addEventListener("click", () => {
    if (coursesPage > 1) {
      coursesPage--;
      refreshCourses();
    }
  });

  $("#courses-next").addEventListener("click", () => {
    coursesPage++;
    refreshCourses();
  });

  refreshCourses();
}

// =======================
// ИНИЦИАЛИЗАЦИЯ
// =======================

window.addEventListener("DOMContentLoaded", () => {
  const page = document.body.dataset.page;

  if (page === "login") initLoginPage();
  if (page === "register") initRegisterPage();
  if (page === "teachers") initTeachersPage();
  if (page === "courses") initCoursesPage();
});
