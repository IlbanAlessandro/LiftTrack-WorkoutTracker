'use strict';

// ── State ─────────────────────────────────────────────────────────────────────
const S = {
    today: new Date(),
    get todayYMD() { return fmtYMD(this.today); },
    allCategories: [],
    allExercises: [],
    selectedCatId: null,
    workoutsYear: new Date().getFullYear(),
    workoutsYears: [],
    progressYear: new Date().getFullYear(),
    progressYears: [],
    // Progress tab — persist across tab switches, reset on year change
    prCategoryId: null,
    prExerciseId: null,
    prExercisesForCategory: [],   // cached exercise list for current PR category
    prDropdownsReady: false,      // have we built the category dropdown yet?
    // Workout modal
    workoutId: null,
    workoutExercises: [],
    inlineMode: null,
};

// ── Bootstrap ─────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('toolbarDate').textContent =
        S.today.toLocaleDateString('en-US', {year:'numeric', month:'long', day:'numeric'});

    document.querySelectorAll('.tab-btn').forEach(b =>
        b.addEventListener('click', () => switchTab(b.dataset.tab)));

    document.querySelectorAll('.modal-overlay').forEach(o =>
        o.addEventListener('click', e => { if (e.target === o) o.style.display = 'none'; }));

    // Block the minus key entirely — negative values never enter the field
    document.addEventListener('keydown', e => {
        if (e.target.type === 'number' && e.key === '-') e.preventDefault();
    });

    // Strip leading zeros as the user types: 021 -> 21, but keep lone 0 and 0.5 valid
    document.addEventListener('input', e => {
        if (e.target.type === 'number' && e.target.value !== '') {
            const n = parseFloat(e.target.value);
            if (!isNaN(n)) e.target.value = n;
        }
    });

    loadDashboard();
});

// ── Tab switching ─────────────────────────────────────────────────────────────
function switchTab(tab) {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.toggle('active', b.dataset.tab === tab));
    document.querySelectorAll('.tab-section').forEach(s => s.classList.toggle('active', s.id === 'tab-' + tab));
    if (tab === 'exercises') loadExercisesTab();
    if (tab === 'workouts')  loadWorkoutsTab();
    if (tab === 'progress')  loadProgressTab();
}

// ── API helper ────────────────────────────────────────────────────────────────
async function api(url, method = 'GET', body = null) {
    const opts = { method, headers: { 'Content-Type': 'application/json' } };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(url, opts);
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || 'Request failed');
    return data;
}

// ── Toast ─────────────────────────────────────────────────────────────────────
function toast(msg, type = 'info', ms = 3000) {
    const t = document.getElementById('toast');
    t.textContent = msg; t.className = 'toast ' + type; t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), ms);
}

// ── Modals ────────────────────────────────────────────────────────────────────
function openModal(id)  { document.getElementById(id).style.display = 'flex'; }
function closeModal(id) { document.getElementById(id).style.display = 'none'; }

function showConfirm(msg, onOk) {
    document.getElementById('confirmMsg').textContent = msg;
    const btn = document.getElementById('confirmOkBtn');
    btn.style.display = '';
    btn.onclick = () => { closeModal('modalConfirm'); onOk(); };
    openModal('modalConfirm');
}

// ════════════════════════════════════════════════════════════════════════════
//  DASHBOARD
// ════════════════════════════════════════════════════════════════════════════
async function loadDashboard() {
    try {
        const d = await api(`/api/dashboard?year=${S.today.getFullYear()}`);
        document.getElementById('dashYear').textContent = `[${d.year}]`;
        document.getElementById('catSetYear').textContent = `[${d.year}]`;
        document.getElementById('dashTotalCount').textContent = d.totalWorkouts;
        renderCalendar(d.workoutDates);
        renderCatSets(d.setsByCategory);
    } catch(e) { console.error('Dashboard error:', e); }
}

function renderCalendar(workoutDates) {
    const now = S.today;
    const year = now.getFullYear(), month = now.getMonth();
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const MONTHS = ['January','February','March','April','May','June','July','August','September','October','November','December'];

    const byKey = {};
    workoutDates.forEach(d => {
        const parts = d.date.split('T')[0].split('-');
        const y = parseInt(parts[0]), mo = parseInt(parts[1]) - 1, dy = parseInt(parts[2]);
        byKey[`${y}-${mo}-${dy}`] = d.workoutId;
    });

    document.getElementById('calendarMonthTitle').textContent = `${MONTHS[month]} ${year}`;
    const grid = document.getElementById('calendarGrid');
    grid.innerHTML = '';

    ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'].forEach(d => {
        const el = document.createElement('div');
        el.className = 'cal-day-header'; el.textContent = d;
        grid.appendChild(el);
    });

    for (let i = 0; i < firstDay; i++) grid.appendChild(document.createElement('div'));

    const todayDate = now.getDate();
    for (let day = 1; day <= daysInMonth; day++) {
        const key = `${year}-${month}-${day}`;
        const isToday = day === todayDate;
        const isPast  = day < todayDate;
        const wid     = byKey[key];

        const cell = document.createElement('div');
        cell.className = 'cal-day';
        if (isToday) cell.classList.add('today');
        if (isPast && !isToday) cell.classList.add('cal-past');

        const num = document.createElement('div');
        num.className = 'cal-day-num';
        num.textContent = day;
        cell.appendChild(num);

        if (wid) {
            const dot = document.createElement('div');
            dot.className = 'cal-dot';
            cell.appendChild(dot);
        }

        if (isToday) {
            cell.classList.add('clickable');
            cell.addEventListener('click', openWorkoutModal);
        } else if (isPast && wid) {
            cell.classList.add('clickable');
            cell.addEventListener('click', () => openReadOnlyModal(wid));
        } else if (isPast && !wid) {
            cell.classList.add('clickable');
            cell.addEventListener('click', () => toast('No workout logged for this day.', 'info'));
        }

        grid.appendChild(cell);
    }
}

function renderCatSets(data) {
    const el = document.getElementById('categorySetsList');
    if (!data || !data.length) {
        el.innerHTML = '<div class="empty-state">No data yet. Start by adding a workout!</div>';
        return;
    }
    const max = Math.max(...data.map(d => d.totalSets));
    el.innerHTML = data.map(d => `
        <div class="cat-set-row">
            <div class="cat-set-label">
                <span class="cat-set-name">${d.categoryName}</span>
                <span class="cat-set-count">${d.totalSets} sets</span>
            </div>
            <div class="cat-set-bar-bg"><div class="cat-set-bar" style="width:${Math.round(d.totalSets/max*100)}%"></div></div>
        </div>`).join('');
}

// ════════════════════════════════════════════════════════════════════════════
//  EXERCISES TAB
// ════════════════════════════════════════════════════════════════════════════
async function loadExercisesTab() {
    try {
        S.allCategories = await api('/api/categories');
        renderCatCards();
        await loadExercises();
    } catch(e) { console.error('Exercises tab error:', e); }
}

function renderCatCards() {
    const row = document.getElementById('categoriesRow');
    row.innerHTML = '';
    row.appendChild(buildCatCard(null, 'All', true, null, S.selectedCatId === null));
    S.allCategories.forEach(c =>
        row.appendChild(buildCatCard(c.id, c.name, c.isDefault, c.exerciseCount, S.selectedCatId === c.id)));
}

function buildCatCard(id, name, isDefault, count, selected) {
    const div = document.createElement('div');
    div.className = 'category-card' + (selected ? ' selected' : '');

    const nameSpan = document.createElement('span');
    nameSpan.className = 'cat-card-name';
    nameSpan.textContent = name;
    div.appendChild(nameSpan);

    if (count !== null) {
        const badge = document.createElement('span');
        badge.className = 'cat-card-count';
        badge.textContent = `(${count})`;
        div.appendChild(badge);
    }

    if (id !== null) {
        const addBtn = document.createElement('button');
        addBtn.className = 'cat-card-add';
        addBtn.title = 'Add exercise';
        addBtn.innerHTML = '<i class="fa-solid fa-plus"></i>';
        addBtn.addEventListener('click', e => { e.stopPropagation(); openAddExerciseModal(id); });
        div.appendChild(addBtn);
    }

    if (id !== null && !isDefault) {
        const delBtn = document.createElement('button');
        delBtn.className = 'cat-card-del';
        delBtn.title = 'Delete category';
        delBtn.innerHTML = '<i class="fa-solid fa-trash-can"></i>';
        delBtn.addEventListener('click', e => {
            e.stopPropagation();
            showConfirm(`Delete category "${name}"?`, async () => {
                try {
                    await api(`/api/categories/${id}`, 'DELETE');
                    toast('Category deleted', 'success');
                    if (S.selectedCatId === id) S.selectedCatId = null;
                    await loadExercisesTab();
                } catch(err) { toast(err.message, 'error'); }
            });
        });
        div.appendChild(delBtn);
    }

    div.addEventListener('click', async e => {
        if (e.target.closest('.cat-card-add') || e.target.closest('.cat-card-del')) return;
        S.selectedCatId = id;
        renderCatCards();
        await loadExercises();
    });

    return div;
}

async function loadExercises() {
    try {
        const url = S.selectedCatId !== null
            ? `/api/exercises?categoryId=${S.selectedCatId}`
            : '/api/exercises';
        S.allExercises = await api(url);

        const cat = S.allCategories.find(c => c.id === S.selectedCatId);
        document.getElementById('exercisesListTitle').textContent =
            cat ? `EXERCISES — ${cat.name}` : 'ALL EXERCISES';

        const el = document.getElementById('exercisesList');
        if (!S.allExercises.length) {
            el.innerHTML = `<div class="empty-state">${S.selectedCatId !== null ? 'No exercises in this category. Click + to add one.' : 'No exercises yet.'}</div>`;
            return;
        }
        el.innerHTML = S.allExercises.map(ex => `
            <div class="exercise-row">
                <div>
                    <span class="exercise-name">${esc(ex.name)}</span>
                    <span class="exercise-cat">${esc(ex.categoryName)}</span>
                </div>
                <button class="btn btn-danger-outline btn-sm" onclick="deleteExercise(${ex.id},'${esc(ex.name)}',${ex.workoutCount})">
                    <i class="fa-solid fa-trash-can"></i> Delete
                </button>
            </div>`).join('');
    } catch(e) { console.error('Load exercises error:', e); }
}

document.getElementById('addCategoryBtn').addEventListener('click', () => {
    document.getElementById('newCategoryName').value = '';
    openModal('modalAddCategory');
});

async function saveCategory() {
    const name = document.getElementById('newCategoryName').value.trim();
    if (!name) { toast('Name is required', 'error'); return; }
    try {
        await api('/api/categories', 'POST', { name });
        closeModal('modalAddCategory');
        toast('Category added', 'success');
        await loadExercisesTab();
    } catch(e) { toast(e.message, 'error'); }
}

function openAddExerciseModal(preCatId = null) {
    document.getElementById('newExerciseName').value = '';
    const sel = document.getElementById('newExerciseCategory');
    sel.innerHTML = S.allCategories.map(c =>
        `<option value="${c.id}"${c.id === preCatId ? ' selected' : ''}>${c.name}</option>`).join('');
    openModal('modalAddExercise');
}

async function saveExercise() {
    const name = document.getElementById('newExerciseName').value.trim();
    const categoryId = parseInt(document.getElementById('newExerciseCategory').value);
    if (!name) { toast('Name is required', 'error'); return; }
    try {
        await api('/api/exercises', 'POST', { name, categoryId });
        closeModal('modalAddExercise');
        toast('Exercise added', 'success');
        await loadExercisesTab();
    } catch(e) { toast(e.message, 'error'); }
}

function deleteExercise(id, name, wcount) {
    if (wcount > 0) {
        document.getElementById('confirmMsg').textContent = `"${name}" is used in ${wcount} workout(s) and cannot be deleted.`;
        document.getElementById('confirmOkBtn').style.display = 'none';
        openModal('modalConfirm');
        return;
    }
    showConfirm(`Delete exercise "${name}"?`, async () => {
        try {
            await api(`/api/exercises/${id}`, 'DELETE');
            toast('Exercise deleted', 'success');
            await loadExercisesTab();
        } catch(e) { toast(e.message, 'error'); }
    });
}

// ════════════════════════════════════════════════════════════════════════════
//  WORKOUTS TAB
// ════════════════════════════════════════════════════════════════════════════
async function loadWorkoutsTab() {
    try {
        S.workoutsYears = await api('/api/workouts/available-years');
        renderWorkoutsYearFilter();
        await loadMonthsGrid();
    } catch(e) { console.error('Workouts tab error:', e); }
}

function renderWorkoutsYearFilter() {
    const el = document.getElementById('workoutsYearFilter');
    el.innerHTML = '';
    S.workoutsYears.forEach(y => {
        const btn = document.createElement('button');
        btn.className = 'year-btn' + (y === S.workoutsYear ? ' selected' : '');
        btn.textContent = y;
        btn.addEventListener('click', async () => {
            S.workoutsYear = y;
            renderWorkoutsYearFilter();
            await loadMonthsGrid();
        });
        el.appendChild(btn);
    });
}

async function loadMonthsGrid() {
    const content = document.getElementById('workoutsContent');
    const workouts = await api(`/api/workouts?year=${S.workoutsYear}`);
    const counts = new Array(12).fill(0);
    workouts.forEach(w => {
        const mo = parseInt(w.date.split('T')[0].split('-')[1]) - 1;
        counts[mo]++;
    });
    const MONTHS = ['January','February','March','April','May','June','July','August','September','October','November','December'];

    if (!workouts.length) {
        content.innerHTML = '<div class="empty-state" style="margin-top:16px">No workouts logged this year.</div>';
        return;
    }

    const grid = document.createElement('div');
    grid.className = 'months-grid';
    MONTHS.forEach((name, i) => {
        const card = document.createElement('div');
        card.className = 'month-card';
        card.innerHTML = `<div class="month-name">${name}</div><div class="month-count${counts[i] ? ' has' : ''}">${counts[i]} workout${counts[i] !== 1 ? 's' : ''}</div>`;
        card.addEventListener('click', () => drillMonth(i + 1));
        grid.appendChild(card);
    });
    content.innerHTML = '';
    content.appendChild(grid);
}

async function drillMonth(month) {
    const content = document.getElementById('workoutsContent');
    const MONTHS = ['','January','February','March','April','May','June','July','August','September','October','November','December'];

    let workouts;
    try { workouts = await api(`/api/workouts?year=${S.workoutsYear}&month=${month}`); }
    catch(e) { toast('Failed to load workouts', 'error'); return; }

    content.innerHTML = '';

    const back = document.createElement('button');
    back.className = 'back-btn';
    back.innerHTML = '<i class="fa-solid fa-chevron-left"></i> BACK TO MONTHS';
    back.addEventListener('click', async () => { await loadMonthsGrid(); });
    content.appendChild(back);

    if (!workouts.length) {
        const empty = document.createElement('div');
        empty.className = 'empty-state';
        empty.textContent = `No workouts in ${MONTHS[month]} ${S.workoutsYear}.`;
        content.appendChild(empty);
        return;
    }

    workouts.forEach(w => {
        const parts = w.date.split('T')[0].split('-');
        const wDate = new Date(parseInt(parts[0]), parseInt(parts[1])-1, parseInt(parts[2]));
        const isToday = fmtYMD(wDate) === S.todayYMD;
        const dateLabel = wDate.toLocaleDateString('en-US', {weekday:'long', month:'long', day:'numeric', year:'numeric'});

        const cats = w.categories.map(c => `<span class="cat-badge">${c.name}</span>`).join('');

        const exMap = {};
        w.exercises.forEach(ex => {
            if (!exMap[ex.exerciseId]) exMap[ex.exerciseId] = {name: ex.exerciseName, sets: []};
            exMap[ex.exerciseId].sets.push(ex);
        });
        const exHtml = Object.values(exMap).map(ex =>
            `<div class="workout-ex-group"><div class="workout-ex-name">${ex.name}</div>${ex.sets.map(s => `<div class="workout-set-row">${setLabel(s)}: ${fmtSet(s)}</div>`).join('')}</div>`
        ).join('');

        const card = document.createElement('div');
        card.className = 'workout-card';
        card.innerHTML = `
            <div class="workout-card-header">
                <div class="workout-date">${dateLabel}</div>
                <div class="workout-actions">
                    ${isToday
                        ? `<button class="btn btn-primary btn-sm" onclick="openWorkoutModal()"><i class="fa-solid fa-pen"></i> Edit</button>
                           <button class="btn btn-danger btn-sm" onclick="deleteWorkout(${w.id})"><i class="fa-solid fa-trash-can"></i> Delete</button>`
                        : `<button class="btn btn-ghost-disabled btn-sm" disabled><i class="fa-solid fa-pen"></i> Edit</button>
                           <button class="btn btn-ghost-disabled btn-sm" disabled><i class="fa-solid fa-trash-can"></i> Delete</button>`}
                </div>
            </div>
            <div class="workout-cats">${cats || '<span style="color:var(--text-dim)">No categories</span>'}</div>
            ${exHtml || '<div class="empty-state" style="padding:8px">No exercises logged.</div>'}`;
        content.appendChild(card);
    });
}

async function deleteWorkout(id) {
    showConfirm('Delete this workout? This cannot be undone.', async () => {
        try {
            await api(`/api/workouts/${id}`, 'DELETE');
            toast('Workout deleted', 'success');
            loadDashboard();
            await loadMonthsGrid();
        } catch(e) { toast(e.message, 'error'); }
    });
}

// ════════════════════════════════════════════════════════════════════════════
//  WORKOUT MODAL (Today)
// ════════════════════════════════════════════════════════════════════════════
async function openWorkoutModal() {
    try {
        if (!S.allCategories.length) S.allCategories = await api('/api/categories');
        // Always fetch full exercise list for filtering
        const allEx = await api('/api/exercises');
        S.allExercises = allEx;

        const workouts = await api(`/api/workouts?year=${S.today.getFullYear()}&month=${S.today.getMonth()+1}`);
        const todayW = workouts.find(w => w.date.split('T')[0] === S.todayYMD);
        S.workoutId = todayW?.id || null;

        document.getElementById('modalWorkoutTitle').textContent =
            `ADD WORKOUT — ${S.today.toLocaleDateString('en-US',{month:'long',day:'numeric',year:'numeric'})}`;

        const selCatIds = new Set((todayW?.categories || []).map(c => c.id));
        document.getElementById('workoutCategories').innerHTML = S.allCategories.map(c =>
            `<label class="checkbox-item${selCatIds.has(c.id)?' checked':''}">
                <input type="checkbox" value="${c.id}"${selCatIds.has(c.id)?' checked':''}>${c.name}
            </label>`).join('');

        document.querySelectorAll('#workoutCategories .checkbox-item').forEach(item => {
            item.addEventListener('click', () => {
                const cb = item.querySelector('input');
                cb.checked = !cb.checked;
                item.classList.toggle('checked', cb.checked);
                // If the inline "new exercise" form is open, re-filter its dropdown immediately
                if (S.inlineMode === 'new') refreshInlineExerciseDropdown();
            });
        });

        S.workoutExercises = [];
        if (todayW) syncFromDto(todayW);

        renderExerciseBlocks();
        cancelInlineForm();
        openModal('modalWorkout');
    } catch(e) { console.error('Open workout modal error:', e); toast('Failed to open workout', 'error'); }
}

function syncFromDto(w) {
    const map = {};
    (w.exercises || []).forEach(ex => {
        if (!map[ex.exerciseId]) map[ex.exerciseId] = {exerciseId: ex.exerciseId, name: ex.exerciseName, sets: []};
        map[ex.exerciseId].sets.push({id:ex.id, sets:ex.sets, reps:ex.reps, weightKg:ex.weightKg, duration:ex.duration, durationUnit:ex.durationUnit, distanceKm:ex.distanceKm});
    });
    S.workoutExercises = Object.values(map);
}

function renderExerciseBlocks() {
    const el = document.getElementById('workoutExercisesContainer');
    if (!S.workoutExercises.length) {
        el.innerHTML = '<div class="empty-state">No exercises added yet.</div>';
        return;
    }
    el.innerHTML = S.workoutExercises.map((ex, exIdx) => `
        <div class="exercise-block">
            <div class="exercise-block-header">
                <span class="exercise-block-name">${esc(ex.name)}</span>
                <div class="exercise-block-actions">
                    <button class="btn btn-primary btn-sm" onclick="showAddSetInline(${ex.exerciseId},'${esc(ex.name)}')">
                        <i class="fa-solid fa-plus"></i> Add Set
                    </button>
                    <button class="btn btn-danger-outline btn-sm" onclick="removeExercise(${ex.exerciseId})">
                        <i class="fa-solid fa-xmark"></i> Delete Exercise
                    </button>
                </div>
            </div>
            ${ex.sets.map(s => `
                <div class="set-row">
                    <span class="set-row-text">${setLabel(s)}: ${fmtSet(s)}</span>
                    <div class="set-row-actions">
                        <button class="btn btn-ghost btn-sm" onclick="openEditSetModal(${ex.exerciseId},${s.id})">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button class="btn btn-danger-outline btn-sm" onclick="removeSet(${s.id})">
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </div>
                </div>`).join('')}
        </div>`).join('');
}

// ── Inline add form ───────────────────────────────────────────────────────────
// Rebuild the exercise dropdown based on currently checked categories.
// Called on open AND whenever a category checkbox is toggled.
function refreshInlineExerciseDropdown() {
    const checkedCatIds = getCheckedCatIds();
    const sel = document.getElementById('inlineExerciseSelect');
    if (checkedCatIds.length === 0) {
        sel.innerHTML = '<option value="">— Select a category first —</option>';
    } else {
        const filtered = S.allExercises.filter(e => checkedCatIds.includes(e.categoryId));
        sel.innerHTML = filtered.length === 0
            ? '<option value="">— No exercises for selected categories —</option>'
            : '<option value="">— Select Exercise —</option>' +
              filtered.map(e => `<option value="${e.id}">${esc(e.name)} (${esc(e.categoryName)})</option>`).join('');
    }
}

function showAddExerciseInline() {
    S.inlineMode = 'new';
    document.getElementById('inlineExerciseRow').style.display = '';
    refreshInlineExerciseDropdown();
    clearInlineFields();
    document.getElementById('addExerciseInline').style.display = 'block';
    document.getElementById('addNewExerciseBtn').style.display = 'none';
}


function showAddSetInline(exerciseId, name) {
    S.inlineMode = {exerciseId, name};
    document.getElementById('inlineExerciseRow').style.display = 'none';
    const ex = S.workoutExercises.find(e => e.exerciseId === exerciseId);
    if (ex?.sets.length) {
        const last = ex.sets[ex.sets.length-1];
        document.getElementById('inlineWeight').value        = last.weightKg    ?? '';
        document.getElementById('inlineReps').value          = last.reps         ?? '';
        document.getElementById('inlineDuration').value      = last.duration     ?? '';
        document.getElementById('inlineDurationUnit').value  = last.durationUnit || 'minutes';
        document.getElementById('inlineDistance').value      = last.distanceKm   ?? '';
    } else { clearInlineFields(); }
    document.getElementById('addExerciseInline').style.display = 'block';
    document.getElementById('addNewExerciseBtn').style.display = 'none';
}

function cancelInlineForm() {
    document.getElementById('addExerciseInline').style.display = 'none';
    document.getElementById('addNewExerciseBtn').style.display = '';
    S.inlineMode = null;
}

function clearInlineFields() {
    ['inlineWeight','inlineReps','inlineDuration','inlineDistance'].forEach(id => document.getElementById(id).value = '');
    document.getElementById('inlineDurationUnit').value = 'minutes';
}

async function submitInlineSet() {
    let exerciseId, exerciseName;
    if (S.inlineMode === 'new') {
        exerciseId = parseInt(document.getElementById('inlineExerciseSelect').value);
        if (!exerciseId) { toast('Please select an exercise', 'error'); return; }
        exerciseName = S.allExercises.find(e => e.id === exerciseId)?.name || '';
    } else {
        exerciseId = S.inlineMode.exerciseId;
        exerciseName = S.inlineMode.name;
    }

    const body = {
        exerciseId,
        reps:        pInt(document.getElementById('inlineReps').value),
        weightKg:    pFloat(document.getElementById('inlineWeight').value),
        duration:    pFloat(document.getElementById('inlineDuration').value),
        durationUnit: document.getElementById('inlineDurationUnit').value,
        distanceKm:  pFloat(document.getElementById('inlineDistance').value),
    };

    if (!S.workoutId) {
        try {
            const w = await api('/api/workouts', 'POST', {date: S.todayYMD, categoryIds: getCheckedCatIds()});
            S.workoutId = w.id;
        } catch(e) { toast(e.message, 'error'); return; }
    }

    try {
        await api(`/api/workouts/${S.workoutId}/sets`, 'POST', body);
        const fresh = await api(`/api/workouts/${S.workoutId}`);
        syncFromDto(fresh);
        cancelInlineForm();
        renderExerciseBlocks();
    } catch(e) { toast(e.message, 'error'); }
}

async function removeSet(weId) {
    if (!S.workoutId) return;
    try {
        await api(`/api/workouts/${S.workoutId}/sets/${weId}`, 'DELETE');
        const fresh = await api(`/api/workouts/${S.workoutId}`);
        syncFromDto(fresh);
        renderExerciseBlocks();
    } catch(e) { toast(e.message, 'error'); }
}

async function removeExercise(exerciseId) {
    const ex = S.workoutExercises.find(e => e.exerciseId === exerciseId);
    if (!ex) return;
    if (!S.workoutId) {
        S.workoutExercises = S.workoutExercises.filter(e => e.exerciseId !== exerciseId);
        renderExerciseBlocks();
        return;
    }
    try {
        for (const s of ex.sets) await api(`/api/workouts/${S.workoutId}/sets/${s.id}`, 'DELETE');
        const fresh = await api(`/api/workouts/${S.workoutId}`);
        syncFromDto(fresh);
        renderExerciseBlocks();
    } catch(e) { toast(e.message, 'error'); }
}

function openEditSetModal(exerciseId, weId) {
    const s = S.workoutExercises.find(e => e.exerciseId === exerciseId)?.sets.find(s => s.id === weId);
    if (!s) return;
    document.getElementById('editSetWeId').value      = weId;
    document.getElementById('editSetWorkoutId').value = S.workoutId;
    document.getElementById('editWeight').value       = s.weightKg    ?? '';
    document.getElementById('editReps').value         = s.reps         ?? '';
    document.getElementById('editDuration').value     = s.duration     ?? '';
    document.getElementById('editDurationUnit').value = s.durationUnit || 'minutes';
    document.getElementById('editDistance').value     = s.distanceKm   ?? '';
    openModal('modalEditSet');
}

async function submitEditSet() {
    const weId = parseInt(document.getElementById('editSetWeId').value);
    const wid  = parseInt(document.getElementById('editSetWorkoutId').value);
    const body = {
        reps:        pInt(document.getElementById('editReps').value),
        weightKg:    pFloat(document.getElementById('editWeight').value),
        duration:    pFloat(document.getElementById('editDuration').value),
        durationUnit: document.getElementById('editDurationUnit').value,
        distanceKm:  pFloat(document.getElementById('editDistance').value),
    };
    try {
        await api(`/api/workouts/${wid}/sets/${weId}`, 'PUT', body);
        const fresh = await api(`/api/workouts/${wid}`);
        syncFromDto(fresh);
        closeModal('modalEditSet');
        renderExerciseBlocks();
        toast('Set updated', 'success');
    } catch(e) { toast(e.message, 'error'); }
}

async function saveWorkout() {
    const catIds = getCheckedCatIds();
    try {
        if (S.workoutId) {
            await api(`/api/workouts/${S.workoutId}`, 'PUT', {categoryIds: catIds});
        } else {
            const w = await api('/api/workouts', 'POST', {date: S.todayYMD, categoryIds: catIds});
            S.workoutId = w.id;
        }
        closeModal('modalWorkout');
        toast('Workout saved', 'success');
        loadDashboard();
    } catch(e) { toast(e.message, 'error'); }
}

function getCheckedCatIds() {
    return [...document.querySelectorAll('#workoutCategories input:checked')].map(cb => parseInt(cb.value));
}

// ════════════════════════════════════════════════════════════════════════════
//  READ-ONLY WORKOUT MODAL
// ════════════════════════════════════════════════════════════════════════════
async function openReadOnlyModal(id) {
    try {
        const w = await api(`/api/workouts/${id}`);
        const parts = w.date.split('T')[0].split('-');
        const wDate = new Date(parseInt(parts[0]), parseInt(parts[1])-1, parseInt(parts[2]));
        document.getElementById('modalReadOnlyTitle').textContent =
            'WORKOUT — ' + wDate.toLocaleDateString('en-US', {weekday:'long', month:'long', day:'numeric', year:'numeric'});

        document.getElementById('roCategories').innerHTML =
            (w.categories || []).map(c => `<span class="cat-badge">${c.name}</span>`).join('') ||
            '<span style="color:var(--text-dim)">No categories</span>';

        const exMap = {};
        (w.exercises || []).forEach(ex => {
            if (!exMap[ex.exerciseId]) exMap[ex.exerciseId] = {name: ex.exerciseName, sets: []};
            exMap[ex.exerciseId].sets.push(ex);
        });
        document.getElementById('roExercises').innerHTML =
            Object.values(exMap).map(ex =>
                `<div class="ro-exercise-group">
                    <div class="ro-exercise-name">${ex.name}</div>
                    ${ex.sets.map(s => `<div class="ro-set-row">${setLabel(s)}: ${fmtSet(s)}</div>`).join('')}
                </div>`
            ).join('') || '<div class="empty-state">No exercises logged.</div>';

        openModal('modalWorkoutReadOnly');
    } catch(e) { console.error('Read-only modal error:', e); toast('Could not load workout', 'error'); }
}

// ════════════════════════════════════════════════════════════════════════════
//  PROGRESS TAB
// ════════════════════════════════════════════════════════════════════════════
async function loadProgressTab() {
    try {
        S.progressYears = await api('/api/workouts/available-years');
        renderProgressYearFilter();
        updateProgressLabels();

        if (!S.allCategories.length) S.allCategories = await api('/api/categories');

        // Build category dropdown only once — preserve state on tab switch
        if (!S.prDropdownsReady) {
            const catSel = document.getElementById('prCategorySelect');
            catSel.innerHTML = '<option value="">— Select Category —</option>' +
                S.allCategories.map(c => `<option value="${c.id}">${c.name}</option>`).join('');

            catSel.onchange = async () => {
                S.prCategoryId  = parseInt(catSel.value) || null;
                S.prExerciseId  = null;
                const exSel = document.getElementById('prExerciseSelect');
                exSel.value = '';

                if (!S.prCategoryId) {
                    exSel.innerHTML = '<option value="">— Select Exercise —</option>';
                    S.prExercisesForCategory = [];
                    clearPrDisplay();
                    return;
                }
                S.prExercisesForCategory = await api(`/api/exercises?categoryId=${S.prCategoryId}`);
                exSel.innerHTML = '<option value="">— Select Exercise —</option>' +
                    S.prExercisesForCategory.map(e => `<option value="${e.id}">${e.name}</option>`).join('');
            };

            document.getElementById('prExerciseSelect').onchange = async () => {
                S.prExerciseId = parseInt(document.getElementById('prExerciseSelect').value) || null;
                await loadPr();
            };

            S.prDropdownsReady = true;
        }

        // Restore persisted state after every tab switch
        await restorePrState();

        await loadFrequency();
        await loadCardio();
    } catch(e) { console.error('Progress tab error:', e); }
}

async function restorePrState() {
    const catSel = document.getElementById('prCategorySelect');
    const exSel  = document.getElementById('prExerciseSelect');

    if (!S.prCategoryId) {
        catSel.value = '';
        exSel.innerHTML = '<option value="">— Select Exercise —</option>';
        clearPrDisplay();
        return;
    }

    // Restore category selection
    catSel.value = S.prCategoryId;

    // Restore exercise list for that category
    if (!S.prExercisesForCategory.length) {
        S.prExercisesForCategory = await api(`/api/exercises?categoryId=${S.prCategoryId}`);
    }
    exSel.innerHTML = '<option value="">— Select Exercise —</option>' +
        S.prExercisesForCategory.map(e => `<option value="${e.id}">${e.name}</option>`).join('');

    if (S.prExerciseId) {
        exSel.value = S.prExerciseId;
        await loadPr();
    }
}

function clearPrDisplay() {
    document.getElementById('prNoData').style.display = 'none';
    document.getElementById('prCurrent').textContent = '—';
    document.getElementById('prBest').textContent = '—';
    window.renderPrChart([], [], '');
}

function renderProgressYearFilter() {
    const el = document.getElementById('progressYearFilter');
    el.innerHTML = '';
    S.progressYears.forEach(y => {
        const btn = document.createElement('button');
        btn.className = 'year-btn' + (y === S.progressYear ? ' selected' : '');
        btn.textContent = y;
        btn.addEventListener('click', async () => {
            S.progressYear = y;
            // Year change: reset PR state entirely
            S.prCategoryId  = null;
            S.prExerciseId  = null;
            S.prExercisesForCategory = [];
            renderProgressYearFilter();
            updateProgressLabels();
            await restorePrState();   // will clear since prCategoryId is null
            await loadFrequency();
            await loadCardio();
        });
        el.appendChild(btn);
    });
}

function updateProgressLabels() {
    document.getElementById('prTitle').textContent    = `PERSONAL RECORDS [${S.progressYear}]`;
    document.getElementById('freqTitle').textContent  = `WORKOUT FREQUENCY [${S.progressYear}]`;
    document.getElementById('cardioTitle').textContent= `CARDIO SUMMARY [${S.progressYear}]`;
    document.getElementById('prBestYearLabel').textContent = S.progressYear;
}

async function loadPr() {
    if (!S.prExerciseId) { clearPrDisplay(); return; }
    try {
        const data = await api(`/api/progress/pr?exerciseId=${S.prExerciseId}&year=${S.progressYear}`);
        const noData = document.getElementById('prNoData');
        if (!data.points?.length) {
            noData.style.display = 'block';
            document.getElementById('prCurrent').textContent = '—';
            document.getElementById('prBest').textContent = '—';
            window.renderPrChart([], [], '');
            return;
        }
        noData.style.display = 'none';
        const labels = data.points.map(p => {
            const pts = p.date.split('T')[0].split('-');
            return new Date(parseInt(pts[0]),parseInt(pts[1])-1,parseInt(pts[2]))
                .toLocaleDateString('en-US',{month:'short',day:'numeric'});
        });
        window.renderPrChart(labels, data.points.map(p => p.value));
        const fmt = r => r ? `${r.display} — ${r.date.split('T')[0]}` : '—';
        document.getElementById('prCurrent').textContent = fmt(data.currentRecord);
        document.getElementById('prBest').textContent    = fmt(data.bestRecord);
    } catch(e) { console.error('PR error:', e); }
}

async function loadFrequency() {
    try {
        const data = await api(`/api/progress/frequency?year=${S.progressYear}`);
        document.getElementById('freqTotal').textContent = data.totalWorkouts;
        document.getElementById('freqAvg').textContent   = data.avgPerMonth.toFixed(1);
        window.renderFreqChart(data.monthlyWorkouts);
    } catch(e) { console.error('Frequency error:', e); }
}

async function loadCardio() {
    try {
        const data = await api(`/api/progress/cardio?year=${S.progressYear}`);
        document.getElementById('cardioTotalDist').textContent = `${(+data.totalDistanceKm).toFixed(1)} km`;
        document.getElementById('cardioTotalDur').textContent  = `${(+data.totalDurationMinutes).toFixed(0)} min`;
        window.renderDistChart(data.monthlyDistance);
        window.renderDurChart(data.monthlyDuration);
    } catch(e) { console.error('Cardio error:', e); }
}

// ════════════════════════════════════════════════════════════════════════════
//  HELPERS
// ════════════════════════════════════════════════════════════════════════════
function fmtYMD(d) {
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
}

// Set label: "Set ×2" when multiple identical sets, "Set 1" / "Set 2" for singles
// i = index in the exercise's sets array (0-based)
function setLabel(s) {
    return s.sets > 1 ? `Sets (${s.sets})` : 'Set';
}

// Format set specs — all combinations handled, no ×N suffix here
function fmtSet(s) {
    const parts  = [];
    const hasW    = s.weightKg   != null && +s.weightKg   !== 0;
    const hasR    = s.reps       != null && +s.reps       !== 0;
    const hasD    = s.duration   != null && +s.duration   !== 0;
    const hasDist = s.distanceKm != null && +s.distanceKm !== 0;

    // Weight or bodyweight indicator
    if (hasW)       parts.push(`${s.weightKg} kg`);
    else if (hasR)  parts.push('bodyweight');

    if (hasR)    parts.push(`${s.reps} reps`);
    if (hasD)    parts.push(`${s.duration} ${s.durationUnit || 'min'}`);
    if (hasDist) parts.push(`${s.distanceKm} km`);

    return parts.length ? parts.join(' × ') : '(empty set)';
}

// Parse helpers — reject negatives, allow 0 for weight (bodyweight marker)
function pInt(v) {
    const n = parseInt(v);
    if (isNaN(n) || n < 0) return null;
    return n === 0 ? null : n;
}
function pFloat(v) {
    const n = parseFloat(v);
    if (isNaN(n) || n < 0) return null;
    return n;   // 0 is valid for weight (bodyweight)
}

function esc(s) {
    return String(s).replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
}
