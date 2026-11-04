let allTasksData = [];
let globalCategories = [];
let globalGroups = [];
let currentGroupId = null;
let currentGroupRole = "Owner";
let sortableInstances = [];

// ---------------------------------------------------------
// XSS ÖNLEME YARDIMCISI (HTML Sanitization)
// ---------------------------------------------------------
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

document.addEventListener('DOMContentLoaded', function () {
    // ---------------------------------------------------------
    // 1. DARK MODE (Karanlık Mod)
    // ---------------------------------------------------------
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');
    const currentTheme = localStorage.getItem('theme') || 'light';
    
    if (currentTheme === 'dark') {
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.body.setAttribute('data-theme', 'dark');
        if (themeIcon) themeIcon.classList.replace('fa-moon', 'fa-sun');
    }
    
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            const isDark = document.documentElement.getAttribute('data-theme') === 'dark' || document.body.getAttribute('data-theme') === 'dark';
            if (isDark) {
                document.documentElement.removeAttribute('data-theme');
                document.documentElement.removeAttribute('data-bs-theme');
                document.body.removeAttribute('data-theme');
                localStorage.setItem('theme', 'light');
                if (themeIcon) themeIcon.classList.replace('fa-sun', 'fa-moon');
            } else {
                document.documentElement.setAttribute('data-theme', 'dark');
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                document.body.setAttribute('data-theme', 'dark');
                localStorage.setItem('theme', 'dark');
                if (themeIcon) themeIcon.classList.replace('fa-moon', 'fa-sun');
            }
        });
    }

    // ---------------------------------------------------------
    // INITIAL LOAD
    // ---------------------------------------------------------
    loadGroups();
    loadProfile();
    loadFriends();

    // ---------------------------------------------------------
    // PROFILE MANAGEMENT
    // ---------------------------------------------------------
    document.getElementById('editProfileForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const newName = document.getElementById('profileNameInput').value;
        fetch('/api/profile/update-info', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: JSON.stringify({ Name: newName })
        }).then(res => {
            if (res.ok) {
                document.getElementById('topProfileName').innerText = newName;
                bootstrap.Modal.getInstance(document.getElementById('editProfileModal')).hide();
                loadProfile(); 
            } else alert("Profil güncellenemedi.");
        });
    });

    document.getElementById('customPicInput').addEventListener('change', function (e) {
        if (!e.target.files || e.target.files.length === 0) return;
        const file = e.target.files[0];
        const formData = new FormData();
        formData.append('file', file);

        fetch('/api/profile/upload-picture', {
            method: 'POST',
            headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: formData
        }).then(res => res.json()).then(data => {
            if (data.url) {
                updateProfilePicUI(data.url);
            }
        }).catch(err => alert("Fotoğraf yüklenemedi."));
    });

    renderStockAvatars();

    // ---------------------------------------------------------
    // EVENT LISTENERS
    // ---------------------------------------------------------
    document.getElementById('createGroupForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const groupName = document.getElementById('newGroupName').value;

        fetch('/api/workgroup', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: JSON.stringify({ Name: groupName })
        }).then(response => {
            if (response.ok) {
                document.getElementById('newGroupName').value = '';
                bootstrap.Modal.getInstance(document.getElementById('createGroupModal')).hide();
                loadGroups(); 
            } else alert("Oda eklenirken hata oluştu.");
        });
    });

    document.getElementById('addCategoryForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const catName = document.getElementById('newCategoryName').value;

        fetch('/api/category', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: JSON.stringify({ Name: catName, WorkGroupId: currentGroupId })
        }).then(response => {
            if (response.ok) {
                document.getElementById('newCategoryName').value = '';
                loadCategories(); 
            } else response.text().then(msg => alert("Kategori eklenirken hata: " + msg));
        });
    });

    document.getElementById('addTaskForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const title = document.getElementById('taskTitle').value;
        const desc = document.getElementById('taskDesc').value;
        const categoryId = document.getElementById('taskCategory').value;
        const priority = document.getElementById('taskPriority').value;
        const dueDate = document.getElementById('taskDueDate').value;

        fetch('/api/task', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: JSON.stringify({
                Title: title,
                Description: desc,
                CategoryId: categoryId ? parseInt(categoryId) : null,
                WorkGroupId: currentGroupId,
                Priority: priority,
                DueDate: dueDate ? dueDate : null,
                IsDone: false
            })
        }).then(response => {
            if (response.ok) {
                document.getElementById('addTaskForm').reset();
                bootstrap.Modal.getInstance(document.getElementById('addTaskModal')).hide();
                loadTasks();
            } else response.text().then(msg => alert("Görev eklenirken hata: " + msg));
        });
    });

    document.getElementById('editTaskForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const id = document.getElementById('editTaskId').value;
        const title = document.getElementById('editTaskTitle').value;
        const desc = document.getElementById('editTaskDesc').value;
        const categoryId = document.getElementById('editTaskCategory').value;
        const priority = document.getElementById('editTaskPriority').value;
        const dueDate = document.getElementById('editTaskDueDate').value;

        fetch(`/api/task/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
            body: JSON.stringify({
                Id: parseInt(id),
                Title: title,
                Description: desc,
                CategoryId: categoryId ? parseInt(categoryId) : null,
                WorkGroupId: currentGroupId,
                Priority: priority,
                DueDate: dueDate ? dueDate : null
            })
        }).then(response => {
            if (response.ok) {
                bootstrap.Modal.getInstance(document.getElementById('editTaskModal')).hide();
                loadTasks();
            } else response.text().then(msg => alert("Görev güncellenirken hata: " + msg));
        });
    });

    document.getElementById('btnToggleStatus').addEventListener('click', function() {
        const id = document.getElementById('editTaskId').value;
        
        fetch(`/api/task/${id}/toggle`, { 
            method: 'PATCH',
            headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
        }).then(res => res.json()).then(task => { 
            bootstrap.Modal.getInstance(document.getElementById('editTaskModal')).hide();
            
            // 3. CONFETTI (Başarı Animasyonu)
            if (task.isDone && typeof confetti === 'function') {
                confetti({
                    particleCount: 150,
                    spread: 80,
                    origin: { y: 0.6 },
                    colors: ['#6366f1', '#10b981', '#f59e0b', '#f43f5e']
                });
            }
            
            loadTasks();
        }).catch(err => alert("Hata: " + err));
    });

    document.getElementById('btnDeleteTask').addEventListener('click', function() {
        const id = document.getElementById('editTaskId').value;
        if (confirm("Bu görevi silmek istediğinize emin misiniz?")) {
            fetch(`/api/task/${id}`, { 
                method: 'DELETE',
                headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
            }).then(res => { 
                if (res.ok) {
                    bootstrap.Modal.getInstance(document.getElementById('editTaskModal')).hide();
                    loadTasks(); 
                } else res.text().then(msg => alert("Hata: " + msg));
            });
        }
    });

    if (document.getElementById('shareGroupForm')) {
        document.getElementById('shareGroupForm').addEventListener('submit', function (e) {
            e.preventDefault();
            const userIdStr = document.getElementById('shareUsername').value;
            const role = document.getElementById('shareRole').value;

            if (!userIdStr) return alert("Lütfen bir arkadaş seçin.");

            fetch(`/api/workgroup/${currentGroupId}/members`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
                body: JSON.stringify({ UserId: parseInt(userIdStr), Role: role })
            }).then(response => {
                if (response.ok) {
                    openGroupManageModal(); 
                } else response.text().then(msg => alert("Hata: " + msg));
            });
        });
    }
});

function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

function loadGroups() {
    fetch('/api/workgroup')
        .then(response => response.json())
        .then(data => {
            globalGroups = data;
            renderSidebar();
            selectGroup(null);
        });
}

function renderSidebar() {
    const sidebar = document.getElementById('sidebarGroupList');
    let html = '';

    globalGroups.forEach(g => {
        let isActive = currentGroupId === g.id ? 'active' : '';
        let icon = g.isOwner ? 'fa-crown text-yellow-duo' : 'fa-users';
        html += `<a href="#" class="duo-nav-link ${isActive}" onclick="selectGroup(${g.id}); return false;"><i class="fas ${icon}"></i> ${escapeHtml(g.name)}</a>`;
    });

    sidebar.innerHTML = html;
}

function selectGroup(id) {
    if (id === null) {
        currentGroupId = null;
        currentGroupRole = "Owner";
        document.getElementById('topbarTitle').innerText = "Kişisel Alanım";
        document.getElementById('nav-group-personal').classList.add('active');
        document.getElementById('btnGroupManage').classList.add('d-none');
    } else {
        const group = globalGroups.find(g => g.id === id);
        if (!group) return;
        currentGroupId = id;
        currentGroupRole = group.role || "Viewer";
        document.getElementById('topbarTitle').innerText = group.name;
        document.getElementById('nav-group-personal').classList.remove('active');
        
        const btnManage = document.getElementById('btnGroupManage');
        if (group.isOwner) btnManage.classList.remove('d-none');
        else btnManage.classList.add('d-none');
    }

    renderSidebar();
    loadWorkspace();
}

function openGroupManageModal() {
    if (!currentGroupId) return;
    const listContainer = document.getElementById('sharedUsersList');
    listContainer.innerHTML = '<div class="text-center"><div class="spinner-border text-primary-duo" role="status"></div></div>';
    
    var modal = bootstrap.Modal.getInstance(document.getElementById('shareGroupModal')) || new bootstrap.Modal(document.getElementById('shareGroupModal'));
    modal.show();

    // Populate friends dropdown
    const select = document.getElementById('shareUsername');
    if (select) {
        select.innerHTML = '<option value="" disabled selected>Yükleniyor...</option>';
        fetch('/api/friends')
            .then(res => res.json())
            .then(data => {
                select.innerHTML = '<option value="" disabled selected>Bir arkadaş seçin</option>';
                if (data.friends && data.friends.length > 0) {
                    data.friends.forEach(f => {
                        let safeName = f.user.username || 'Kullanıcı';
                        select.innerHTML += `<option value="${f.user.id}">${escapeHtml(safeName)}</option>`;
                    });
                } else {
                    select.innerHTML = '<option value="" disabled>Arkadaşınız bulunmuyor</option>';
                }
            })
            .catch(() => {
                select.innerHTML = '<option value="" disabled>Arkadaş listesi alınamadı</option>';
            });
    }

    fetch(`/api/workgroup/${currentGroupId}/members`)
        .then(res => res.json())
        .then(data => {
            listContainer.innerHTML = '';
            if (!data || data.length === 0) {
                listContainer.innerHTML = '<div class="text-muted text-center fw-bold">Bu odada sadece siz varsınız.</div>';
                return;
            }
            
            data.forEach(user => {
                let badgeClass = user.isOwner ? 'bg-warning text-dark' : (user.role === 'Admin' ? 'bg-blue-duo' : 'bg-secondary text-white');
                let roleStr = user.isOwner ? 'Kurucu' : (user.role === 'Admin' ? 'Düzenleyici' : 'İzleyici');
                
                let actionBtn = '';
                if (currentGroupRole === 'Owner' && !user.isOwner) {
                    actionBtn = `<button class="btn btn-sm btn-outline-danger p-1 px-2 ms-2" title="Odadan Çıkar" onclick="removeGroupMember(${user.userId})"><i class="fas fa-user-times"></i></button>`;
                }

                listContainer.innerHTML += `
                    <div class="d-flex justify-content-between align-items-center mb-2 p-2" style="border: 1px solid var(--border-color); border-radius: var(--radius-md);">
                        <span class="fw-bold" style="color: var(--text-main);">${escapeHtml(user.username)}</span>
                        <div class="d-flex align-items-center">
                            <span class="badge ${badgeClass}">${roleStr}</span>
                            ${actionBtn}
                        </div>
                    </div>
                `;
            });
        });
}

function removeGroupMember(memberUserId) {
    if (!confirm("Bu üyeyi odadan çıkarmak istediğinize emin misiniz?")) return;
    fetch(`/api/workgroup/${currentGroupId}/members/${memberUserId}`, {
        method: 'DELETE',
        headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
    }).then(res => {
        if (res.ok) {
            openGroupManageModal();
        } else {
            res.text().then(msg => alert("Hata: " + msg));
        }
    }).catch(err => alert("Bağlantı hatası: " + err));
}

function updateCategoriesSelectUI(data) {
    const addSelect = document.getElementById('taskCategory');
    const editSelect = document.getElementById('editTaskCategory');
    const modalListContainer = document.getElementById('modalCategoryListContainer');

    if (addSelect) addSelect.innerHTML = '<option value="">Genel (Kategorisiz)</option>';
    if (editSelect) editSelect.innerHTML = '<option value="">Genel (Kategorisiz)</option>';
    if (modalListContainer) modalListContainer.innerHTML = '';
    let canEdit = currentGroupRole !== 'Viewer';

    if (data.length === 0 && modalListContainer) {
        modalListContainer.innerHTML = '<div class="text-muted text-center py-3 fw-bold">Kategori yok.</div>';
    }

    data.forEach(cat => {
        let optionHtml = '<option value="' + cat.id + '">' + escapeHtml(cat.name) + '</option>';
        if (addSelect) addSelect.innerHTML += optionHtml;
        if (editSelect) editSelect.innerHTML += optionHtml;

        let actionButtons = canEdit ? '<button type="button" class="btn btn-sm btn-outline-danger border-0 p-1" onclick="deleteCategory(' + cat.id + ')" title="Sil" style="background:transparent;"><i class="fas fa-trash text-danger"></i></button>' : '<span class="badge bg-secondary">İzleyici</span>';
        if (modalListContainer) {
            modalListContainer.innerHTML += '<div class="category-list-item"><span><i class="fas fa-tag me-2 text-primary-duo"></i>' + escapeHtml(cat.name) + '</span><div>' + actionButtons + '</div></div>';
        }
    });
}

function loadCategories() {
    let url = currentGroupId ? ('/api/category?groupId=' + currentGroupId) : '/api/category';
    return fetch(url)
        .then(response => response.json())
        .then(data => {
            globalCategories = data;
            updateCategoriesSelectUI(data);
        });
}

function loadWorkspace() {
    let catUrl = currentGroupId ? ('/api/category?groupId=' + currentGroupId) : '/api/category';
    let taskUrl = currentGroupId ? ('/api/task?groupId=' + currentGroupId) : '/api/task';

    // PARALLEL FETCH: Kategori ve görev istekleri eşzamanlı gönderilerek waterfall gecikmesi sıfırlanır
    Promise.all([
        fetch(catUrl).then(r => r.json()),
        fetch(taskUrl).then(r => r.json())
    ]).then(([cats, tasks]) => {
        globalCategories = cats;
        updateCategoriesSelectUI(cats);
        renderTasksData(tasks);
    }).catch(err => {
        console.error('Workspace yüklenirken hata:', err);
    });
}

function deleteCategory(id) {
    if (confirm("Kategoriyi silmek istediğinize emin misiniz?")) {
        fetch(`/api/category/${id}`, { method: 'DELETE', headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() } })
            .then(res => { if (res.ok) loadCategories(); else res.text().then(msg => alert("Hata: " + msg)); });
    }
}

function loadTasks() {
    let url = currentGroupId ? ('/api/task?groupId=' + currentGroupId) : '/api/task';
    fetch(url)
        .then(response => response.json())
        .then(data => {
            renderTasksData(data);
        });
}

function renderTasksData(data) {
    allTasksData = data;
    // Clean up SortableJS instances
            sortableInstances.forEach(s => s.destroy());
            sortableInstances = [];

            const total = data.length;
            const done = data.filter(t => t.isDone).length;
            const active = total - done;
            const high = data.filter(t => t.priority === 'Yüksek' && !t.isDone).length;

            if(document.getElementById('statTotal')) document.getElementById('statTotal').innerText = total;
            if(document.getElementById('statActive')) document.getElementById('statActive').innerText = active;
            if(document.getElementById('statDone')) document.getElementById('statDone').innerText = done;
            if(document.getElementById('statHighPriority')) document.getElementById('statHighPriority').innerText = high;

            const pathContainer = document.getElementById('pathContainer');
            pathContainer.innerHTML = '';
            
            let grouped = {};
            grouped[0] = { name: "Genel Görevler", tasks: [] };
            globalCategories.forEach(c => { grouped[c.id] = { name: c.name, tasks: [] }; });
            
            data.forEach(t => {
                if (t.categoryId && grouped[t.categoryId]) grouped[t.categoryId].tasks.push(t);
                else grouped[0].tasks.push(t);
            });
            
            let html = '';
            let canEdit = currentGroupRole !== 'Viewer';

            for (let catId in grouped) {
                let cat = grouped[catId];
                if (cat.tasks.length === 0 && catId == 0) continue; 
                
                html += `
                    <div style="width: 100%; margin-bottom: 40px;">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h4 style="font-weight: 800; color: var(--text-main); margin: 0;">${escapeHtml(cat.name)}</h4>
                            ${canEdit ? `<button class="btn btn-sm btn-outline-info" onclick="openAddTaskWithCat('${catId}')"><i class="fas fa-plus"></i> Görev</button>` : ''}
                        </div>
                        <div class="task-grid" id="grid-${catId}" data-catid="${catId}">
                `;
                
                cat.tasks.forEach((task) => {
                    let cardClass = "duo-task-card";
                    if (task.isDone) cardClass += " completed";
                    if (!task.isDone && task.priority === 'Yüksek') cardClass += " high-priority";
                    
                    let icon = task.isDone 
                        ? `<button class="btn btn-sm btn-link p-0 task-check-btn" onclick="toggleTaskStatusDirect(${task.id}, event)"><i class="fas fa-check-circle text-success fs-3"></i></button>` 
                        : `<button class="btn btn-sm btn-link p-0 task-check-btn" onclick="toggleTaskStatusDirect(${task.id}, event)"><i class="far fa-circle text-secondary fs-3" style="opacity:0.6;"></i></button>`;
                        
                    let priorityBadgeClass = task.priority === 'Yüksek' ? 'bg-red-duo text-white' : (task.priority === 'Düşük' ? 'bg-secondary text-white' : 'bg-blue-duo text-white');

                    // DUE DATE
                    let dueDateHtml = '';
                    if (task.dueDate) {
                        const d = new Date(task.dueDate);
                        const isOverdue = !task.isDone && (d < new Date());
                        const dateString = d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
                        dueDateHtml = `<div class="due-date-badge ${isOverdue ? 'overdue' : ''}"><i class="fas fa-clock"></i> ${escapeHtml(dateString)}</div>`;
                    }

                    html += `
                        <div class="${cardClass}" onclick="openEditModal(${task.id})" data-taskid="${task.id}">
                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <div>${icon}</div>
                                <span class="badge ${priorityBadgeClass}">${escapeHtml(task.priority)}</span>
                            </div>
                            <h4 style="font-weight: 600; margin-bottom: 8px;">${escapeHtml(task.title)}</h4>
                            <p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 12px; flex-grow: 1;">${escapeHtml(task.description || 'Açıklama yok.')}</p>
                            
                            <div class="d-flex justify-content-between align-items-center mt-auto pt-2 border-top border-secondary border-opacity-10">
                                <div style="font-size: 0.8rem; font-weight: 500; color: var(--text-light); text-transform: uppercase;">
                                    <i class="fas fa-folder me-1"></i> ${escapeHtml(cat.name)}
                                </div>
                                ${dueDateHtml}
                            </div>
                        </div>
                    `;
                });
                
                // EMPTY STATES
                if(cat.tasks.length === 0) {
                    html += `
                        <div class="empty-state w-100">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="9" y1="3" x2="9" y2="21"></line><path d="M14 9l2 2 4-4"></path>
                            </svg>
                            <h5>Henüz Görev Yok</h5>
                            <p>Bu kategori şimdilik boş. Hadi yeni bir görev ekleyerek çalışmaya başla!</p>
                            ${canEdit ? `<button class="btn btn-sm btn-primary-custom mt-2" onclick="openAddTaskWithCat('${catId}')"><i class="fas fa-plus"></i> Görev Ekle</button>` : ''}
                        </div>
                    `;
                }

                html += `</div></div>`;
            }
            
            if (data.length === 0) {
                 html = `
                    <div class="empty-state w-100 mt-5" style="grid-column: 1 / -1;">
                        <svg style="width:160px; height:160px; color: var(--primary-bg);" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                            <circle cx="12" cy="12" r="10"></circle><path d="M8 14s1.5 2 4 2 4-2 4-2"></path><line x1="9" y1="9" x2="9.01" y2="9"></line><line x1="15" y1="9" x2="15.01" y2="9"></line>
                        </svg>
                        <h3 class="fw-bold text-main mt-3">Her Şey Tertemiz!</h3>
                        <p class="text-muted">Burada henüz hiç görev yok. Yeni görevler oluşturarak harika bir gün planlayabilirsin.</p>
                        ${canEdit ? `<button class="btn btn-primary-custom mt-3 px-4" data-bs-toggle="modal" data-bs-target="#addTaskModal"><i class="fas fa-plus"></i> İlk Görevi Ekle</button>` : ''}
                    </div>
                 `;
            }
            
            pathContainer.innerHTML = html;

            // SORTABLE JS (Drag and Drop)
            if (sortableInstances && sortableInstances.length > 0) {
                sortableInstances.forEach(s => {
                    try { s.destroy(); } catch (e) {}
                });
                sortableInstances = [];
            }

            if (canEdit && typeof Sortable !== 'undefined') {
                const grids = document.querySelectorAll('.task-grid');
                grids.forEach(grid => {
                    const sortable = new Sortable(grid, {
                        group: 'shared', 
                        animation: 150,
                        ghostClass: 'sortable-ghost',
                        onEnd: function (evt) {
                            const itemEl = evt.item; 
                            if (itemEl.classList.contains('empty-state')) return;
                            
                            const toGrid = evt.to;
                            const newCatId = toGrid.getAttribute('data-catid');
                            const taskId = itemEl.getAttribute('data-taskid');
                            
                            updateTaskCategory(taskId, newCatId);
                        },
                    });
                    sortableInstances.push(sortable);
                });
            }
        });
}

function formatDateTimeLocal(dateString) {
    if (!dateString) return '';
    const d = new Date(dateString);
    if (isNaN(d.getTime())) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function updateTaskCategory(taskId, newCategoryId) {
    const task = allTasksData.find(t => t.id == taskId);
    if (!task) return;

    fetch(`/api/task/${taskId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
        body: JSON.stringify({
            Id: task.id,
            Title: task.title,
            Description: task.description,
            CategoryId: newCategoryId == 0 ? null : parseInt(newCategoryId),
            WorkGroupId: task.workGroupId,
            Priority: task.priority,
            DueDate: task.dueDate
        })
    }).then(response => {
        if (!response.ok) {
            response.text().then(msg => alert("Görev taşınırken hata: " + msg));
            loadTasks(); 
        } else {
            loadTasks();
        }
    });
}

function openAddTaskWithCat(catId) {
    document.getElementById('taskCategory').value = (catId != 0) ? catId : "";
    const addModalEl = document.getElementById('addTaskModal');
    var addModal = bootstrap.Modal.getInstance(addModalEl) || new bootstrap.Modal(addModalEl);
    addModal.show();
}

function openEditModal(id) {
    const task = allTasksData.find(t => t.id === id);
    if (task) {
        document.getElementById('editTaskId').value = task.id;
        document.getElementById('editTaskTitle').value = task.title;
        document.getElementById('editTaskDesc').value = task.description || '';
        document.getElementById('editTaskCategory').value = task.categoryId || '';
        document.getElementById('editTaskPriority').value = task.priority || 'Orta';
        
        document.getElementById('editTaskDueDate').value = formatDateTimeLocal(task.dueDate);

        let btnToggle = document.getElementById('btnToggleStatus');
        if (task.isDone) {
            btnToggle.innerHTML = '<i class="fas fa-times"></i> Geri Al';
            btnToggle.style.background = 'var(--gray-bg)';
            btnToggle.style.color = 'var(--text-muted)';
        } else {
            btnToggle.innerHTML = '<i class="fas fa-check"></i> Tamamla';
            btnToggle.style.background = 'var(--success-bg)';
            btnToggle.style.color = 'white';
        }
        
        let canEdit = currentGroupRole !== 'Viewer';
        document.getElementById('btnToggleStatus').style.display = canEdit ? 'block' : 'none';
        document.getElementById('btnDeleteTask').style.display = canEdit ? 'block' : 'none';
        document.getElementById('editTaskForm').querySelector('button[type="submit"]').style.display = canEdit ? 'block' : 'none';

        if (typeof loadComments === 'function') loadComments(id);

        const editModalEl = document.getElementById('editTaskModal');
        var editModal = bootstrap.Modal.getInstance(editModalEl) || new bootstrap.Modal(editModalEl);
        editModal.show();
    }
}

function loadProfile() {
    fetch('/api/profile/me')
        .then(res => res.json())
        .then(user => {
            const displayName = user.name || (user.email ? user.email.split('@')[0] : 'Kullanıcı');
            document.getElementById('topProfileName').innerText = displayName;
            document.getElementById('profileNameInput').value = user.name || '';
            
            if (user.profilePictureUrl) {
                updateProfilePicUI(user.profilePictureUrl);
            } else {
                let initials = displayName.length >= 2 ? displayName.substring(0, 2).toUpperCase() : displayName.toUpperCase();
                document.getElementById('topProfilePic').innerText = initials;
                document.getElementById('currentModalProfilePic').innerText = initials;
                document.getElementById('topProfilePic').style.backgroundImage = 'none';
                document.getElementById('currentModalProfilePic').style.backgroundImage = 'none';
            }
        })
        .catch(err => console.error("Profil yüklenemedi:", err));
}

function updateProfilePicUI(url) {
    const topPic = document.getElementById('topProfilePic');
    const modalPic = document.getElementById('currentModalProfilePic');
    
    topPic.innerHTML = '';
    modalPic.innerHTML = '';
    
    // Güvenli CSS URL tanımlaması
    const safeUrl = encodeURI(url);
    topPic.style.backgroundImage = `url("${safeUrl}")`;
    topPic.style.backgroundSize = 'cover';
    topPic.style.backgroundPosition = 'center';
    
    modalPic.style.backgroundImage = `url("${safeUrl}")`;
    modalPic.style.backgroundSize = 'cover';
    modalPic.style.backgroundPosition = 'center';
}

function renderStockAvatars() {
    const container = document.getElementById('stockAvatarsContainer');
    const seeds = ['Felix', 'Aneka', 'Bandit', 'Jasper', 'Max', 'Milo', 'Luna', 'Lily'];
    
    let html = '';
    seeds.forEach(seed => {
        let url = `https://api.dicebear.com/7.x/adventurer/svg?seed=${seed}`;
        html += `
            <div class="stock-avatar-option" style="width:60px; height:60px; border-radius:50%; cursor:pointer; background-image:url('${encodeURI(url)}'); background-size:cover; border: 2px solid transparent; transition:0.2s;" 
                 onclick="setStockAvatar('${encodeURI(url)}', this)"
                 onmouseover="this.style.transform='scale(1.1)'"
                 onmouseout="this.style.transform='scale(1)'">
            </div>
        `;
    });
    container.innerHTML = html;
}

function setStockAvatar(url, element) {
    fetch('/api/profile/set-stock-picture', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
        body: JSON.stringify(url)
    }).then(res => res.json()).then(data => {
        if (data.url) {
            updateProfilePicUI(data.url);
        }
    });
}

function toggleTaskStatusDirect(id, event) {
    event.stopPropagation();
    fetch('/api/task/' + id + '/toggle', { 
        method: 'PATCH',
        headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
    }).then(res => res.json()).then(task => { 
        if (task.isDone && typeof confetti === 'function') {
            confetti({
                particleCount: 150,
                spread: 80,
                origin: { y: 0.6 },
                colors: ['#6366f1', '#10b981', '#f59e0b', '#f43f5e']
            });
        }
        loadTasks();
    }).catch(err => alert("Hata: " + err));
}

// ---------------------------------------------------------
// COMMENTS LOGIC
// ---------------------------------------------------------
function loadComments(taskId) {
    const container = document.getElementById('commentsList');
    if (!container) return;
    
    container.innerHTML = '<div class="text-center"><div class="spinner-border spinner-border-sm text-primary"></div></div>';
    
    fetch('/api/comments/' + taskId)
        .then(res => res.json())
        .then(data => {
            container.innerHTML = '';
            if (data.length === 0) {
                container.innerHTML = '<div class="text-muted text-center small">Henüz yorum yok. İlk yorumu sen yap!</div>';
                return;
            }
            
            data.forEach(c => {
                let safeName = c.user.username || '??';
                let picHtml = c.user.profilePictureUrl 
                    ? `<div style="width:30px;height:30px;border-radius:50%;background-image:url('${encodeURI(c.user.profilePictureUrl)}');background-size:cover;"></div>`
                    : `<div style="width:30px;height:30px;border-radius:50%;background:var(--primary-bg);color:white;display:flex;align-items:center;justify-content:center;font-size:12px;">${escapeHtml(safeName.substring(0,2).toUpperCase())}</div>`;
                    
                const d = new Date(c.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
                
                container.innerHTML += `
                    <div class="d-flex gap-2 mb-2">
                        ${picHtml}
                        <div class="p-2 rounded-3" style="flex:1; background: var(--bg-main); border: 1px solid var(--border-color); color: var(--text-main);">
                            <div class="d-flex justify-content-between"><strong style="font-size:0.85rem; color: var(--text-main);">${escapeHtml(safeName)}</strong> <small class="text-muted" style="font-size:0.75rem;">${escapeHtml(d)}</small></div>
                            <div style="font-size:0.9rem; color: var(--text-main);">${escapeHtml(c.content)}</div>
                        </div>
                    </div>
                `;
            });
            container.scrollTop = container.scrollHeight;
        });
}

function sendComment() {
    const taskId = document.getElementById('editTaskId').value;
    const input = document.getElementById('newCommentInput');
    const content = input.value;
    
    if(!content.trim()) return;
    
    fetch('/api/comments/' + taskId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': getAntiForgeryToken() },
        body: JSON.stringify(content)
    }).then(res => {
        if(res.ok) {
            input.value = '';
            loadComments(taskId);
        }
    });
}

// ---------------------------------------------------------
// FRIENDSHIP LOGIC
// ---------------------------------------------------------
document.getElementById('friendsModal')?.addEventListener('show.bs.modal', function () {
    loadFriends();
});

function searchFriends() {
    const query = document.getElementById('friendSearchInput').value;
    const container = document.getElementById('friendSearchResults');
    
    if (!query || !query.trim()) return;
    
    container.innerHTML = '<div class="text-center mt-2"><div class="spinner-border spinner-border-sm text-primary"></div></div>';
    
    fetch('/api/friends/search?query=' + encodeURIComponent(query.trim()))
        .then(res => res.json())
        .then(data => {
            container.innerHTML = '';
            if (!data || data.length === 0) {
                container.innerHTML = '<div class="text-muted text-center small mt-2">Kişi bulunamadı.</div>';
                return;
            }
            
            data.forEach(u => {
                let safeName = u.username || (u.email ? u.email.split('@')[0] : 'Kullanıcı');
                let pic = u.profilePictureUrl 
                    ? `<img src="${encodeURI(u.profilePictureUrl)}" style="width:32px;height:32px;border-radius:50%;object-fit:cover;">` 
                    : `<div style="width:32px;height:32px;border-radius:50%;background:var(--primary-bg);color:white;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;">${escapeHtml(safeName.substring(0,2).toUpperCase())}</div>`;
                
                let actionHtml = '';
                if (u.status === 'Friend') {
                    actionHtml = '<span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1"><i class="fas fa-check me-1"></i>Arkadaşınız</span>';
                } else if (u.status === 'PendingSent') {
                    actionHtml = '<span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2 py-1"><i class="fas fa-clock me-1"></i>İstek Gönderildi</span>';
                } else if (u.status === 'PendingReceived') {
                    actionHtml = `<button class="btn btn-sm btn-success px-2 py-1" onclick="acceptFriendRequest(${u.friendshipId})"><i class="fas fa-check me-1"></i>Kabul Et</button>`;
                } else {
                    actionHtml = `<button class="btn btn-sm btn-outline-primary px-2 py-1" onclick="sendFriendRequest(${u.id})"><i class="fas fa-user-plus me-1"></i>Ekle</button>`;
                }

                container.innerHTML += `
                    <div class="d-flex justify-content-between align-items-center mb-2 p-2 border rounded" style="background: var(--bg-main); border-color: var(--border-color) !important;">
                        <div class="d-flex align-items-center gap-2">
                            ${pic}
                            <div>
                                <span class="fw-bold" style="font-size:0.9rem; color: var(--text-main);">${escapeHtml(safeName)}</span>
                                ${u.email ? `<div class="text-muted" style="font-size:0.75rem;">${escapeHtml(u.email)}</div>` : ''}
                            </div>
                        </div>
                        <div>
                            ${actionHtml}
                        </div>
                    </div>
                `;
            });
        })
        .catch(err => {
            container.innerHTML = '<div class="text-danger text-center small mt-2">Arama yapılırken hata oluştu.</div>';
        });
}

function sendFriendRequest(userId) {
    fetch('/api/friends/request/' + userId, {
        method: 'POST',
        headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
    }).then(res => {
        return res.json().then(data => ({ ok: res.ok, status: res.status, data })).catch(() => ({ ok: res.ok, status: res.status, data: {} }));
    }).then(result => {
        if (result.ok) {
            alert(result.data.message || "İstek başarıyla gönderildi.");
            searchFriends();
            loadFriends();
        } else {
            alert(result.data?.message || "Hata: İstek gönderilemedi.");
        }
    }).catch(err => {
        alert("Bağlantı hatası: " + err);
    });
}

function acceptFriendRequest(id) {
    fetch('/api/friends/accept/' + id, {
        method: 'POST',
        headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
    }).then(res => {
        if (res.ok) {
            loadFriends();
            const query = document.getElementById('friendSearchInput')?.value;
            if (query && query.trim()) searchFriends();
        } else {
            res.text().then(msg => alert("Hata: " + msg));
        }
    }).catch(err => alert("Bağlantı hatası: " + err));
}

function declineOrDeleteFriend(id, isFriend = false) {
    if (isFriend && !confirm("Bu kişiyi arkadaş listenizden çıkarmak istediğinize emin misiniz?")) {
        return;
    }
    fetch('/api/friends/' + id, {
        method: 'DELETE',
        headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
    }).then(res => {
        if (res.ok) {
            loadFriends();
            const query = document.getElementById('friendSearchInput')?.value;
            if (query && query.trim()) searchFriends();
        } else {
            res.text().then(msg => alert("Hata: " + msg));
        }
    }).catch(err => alert("Bağlantı hatası: " + err));
}

function loadFriends() {
    fetch('/api/friends')
        .then(res => res.json())
        .then(data => {
            const pendingCount = (data.pendingRequests || []).length;
            const sentCount = (data.sentRequests || []).length;
            const friendsCount = (data.friends || []).length;

            // Sidebar Badge (Sol menüdeki bildirim)
            const sidebarBadge = document.getElementById('sidebarFriendBadge');
            if (sidebarBadge) {
                if (pendingCount > 0) {
                    sidebarBadge.innerText = pendingCount;
                    sidebarBadge.classList.remove('d-none');
                } else {
                    sidebarBadge.classList.add('d-none');
                }
            }

            // Modal sayaçları
            const pendingBadge = document.getElementById('pendingReqCount');
            if (pendingBadge) pendingBadge.innerText = pendingCount;
            
            const sentBadge = document.getElementById('sentReqCount');
            if (sentBadge) sentBadge.innerText = sentCount;

            const friendsBadge = document.getElementById('friendsCount');
            if (friendsBadge) friendsBadge.innerText = friendsCount;

            // 1. Gelen İstekler Listesi
            const pendingList = document.getElementById('pendingRequestsList');
            if (pendingList) {
                pendingList.innerHTML = pendingCount === 0 
                    ? '<div class="text-muted text-center small mt-2">Gelen bekleyen istek yok.</div>' 
                    : '';
                data.pendingRequests.forEach(r => {
                    let safeName = r.user.username || (r.user.email ? r.user.email.split('@')[0] : 'Kullanıcı');
                    let pic = r.user.profilePictureUrl 
                        ? `<img src="${encodeURI(r.user.profilePictureUrl)}" style="width:32px;height:32px;border-radius:50%;object-fit:cover;">` 
                        : `<div style="width:32px;height:32px;border-radius:50%;background:var(--primary-bg);color:white;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;">${escapeHtml(safeName.substring(0,2).toUpperCase())}</div>`;
                    
                    pendingList.innerHTML += `
                        <div class="d-flex justify-content-between align-items-center mb-2 p-2 border rounded" style="background: var(--bg-main); border-color: var(--border-color) !important;">
                            <div class="d-flex align-items-center gap-2">
                                ${pic}
                                <span class="fw-bold" style="font-size:0.9rem; color: var(--text-main);">${escapeHtml(safeName)}</span>
                            </div>
                            <div class="d-flex gap-1">
                                <button class="btn btn-sm btn-success p-1 px-2" title="Kabul Et" onclick="acceptFriendRequest(${r.id})"><i class="fas fa-check"></i></button>
                                <button class="btn btn-sm btn-danger p-1 px-2" title="Reddet" onclick="declineOrDeleteFriend(${r.id})"><i class="fas fa-times"></i></button>
                            </div>
                        </div>
                    `;
                });
            }

            // 2. Gönderilen İstekler Listesi
            const sentList = document.getElementById('sentRequestsList');
            if (sentList) {
                sentList.innerHTML = sentCount === 0 
                    ? '<div class="text-muted text-center small mt-2">Gönderilen bekleyen istek yok.</div>' 
                    : '';
                data.sentRequests.forEach(r => {
                    let safeName = r.user.username || (r.user.email ? r.user.email.split('@')[0] : 'Kullanıcı');
                    let pic = r.user.profilePictureUrl 
                        ? `<img src="${encodeURI(r.user.profilePictureUrl)}" style="width:32px;height:32px;border-radius:50%;object-fit:cover;">` 
                        : `<div style="width:32px;height:32px;border-radius:50%;background:var(--primary-bg);color:white;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;">${escapeHtml(safeName.substring(0,2).toUpperCase())}</div>`;
                    
                    sentList.innerHTML += `
                        <div class="d-flex justify-content-between align-items-center mb-2 p-2 border rounded" style="background: var(--bg-main); border-color: var(--border-color) !important;">
                            <div class="d-flex align-items-center gap-2">
                                ${pic}
                                <span class="fw-bold" style="font-size:0.9rem; color: var(--text-main);">${escapeHtml(safeName)}</span>
                            </div>
                            <button class="btn btn-sm btn-outline-secondary p-1 px-2" title="İsteği İptal Et" onclick="declineOrDeleteFriend(${r.id})">
                                <i class="fas fa-undo me-1"></i>İptal
                            </button>
                        </div>
                    `;
                });
            }

            // 3. Kabul Edilmiş Arkadaşlar Listesi
            const friendsList = document.getElementById('acceptedFriendsList');
            if (friendsList) {
                friendsList.innerHTML = friendsCount === 0 
                    ? '<div class="text-muted text-center mt-5"><i class="fas fa-user-slash fs-1 mb-3 opacity-50"></i><br/>Henüz hiç arkadaşınız yok.</div>' 
                    : '';
                data.friends.forEach(f => {
                    let safeName = f.user.username || (f.user.email ? f.user.email.split('@')[0] : 'Kullanıcı');
                    let pic = f.user.profilePictureUrl 
                        ? `<img src="${encodeURI(f.user.profilePictureUrl)}" style="width:40px;height:40px;border-radius:50%;object-fit:cover;">` 
                        : `<div style="width:40px;height:40px;border-radius:50%;background:var(--primary-bg);color:white;display:flex;align-items:center;justify-content:center;font-size:16px;font-weight:700;">${escapeHtml(safeName.substring(0,2).toUpperCase())}</div>`;
                    
                    friendsList.innerHTML += `
                        <div class="d-flex justify-content-between align-items-center mb-3 p-3 border rounded shadow-sm" style="background: var(--bg-main); border-color: var(--border-color) !important;">
                            <div class="d-flex align-items-center gap-3">
                                ${pic}
                                <div>
                                    <div class="fw-bold" style="font-size:1rem; color: var(--text-main);">${escapeHtml(safeName)}</div>
                                    ${f.user.email ? `<small class="text-muted">${escapeHtml(f.user.email)}</small>` : ''}
                                </div>
                            </div>
                            <button class="btn btn-sm btn-outline-danger" title="Arkadaşlıktan Çıkar" onclick="declineOrDeleteFriend(${f.id}, true)"><i class="fas fa-user-minus"></i></button>
                        </div>
                    `;
                });
            }
        })
        .catch(err => console.error("Arkadaş listesi yüklenemedi:", err));
}
