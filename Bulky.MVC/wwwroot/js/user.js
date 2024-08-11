let dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable(status) {
    dataTable = $('#tableData').DataTable({
        "ajax": { url: `/admin/user/getall` },
        "columns": [
            { data: 'name', width: "15%" },
            { data: 'email', width: "20%" },
            { data: 'phoneNumber', width: "15%" },
            { data: 'company.name', width: "10%" },
            { data: 'role', width: "10%" },
            {
                data: { id: 'id', lockoutEnd: 'lockoutEnd' },
                'render': function (data) {
                    let today = new Date().getTime();
                    let lockout = new Date(data.lockoutEnd).getTime();
                    if (lockout > today) {
                        return `
                        <div class="text-center">
                            <a onclick="lockUnlock('${data.id}')" class="btn btn-success text-white mx-2" style="cursor: pointer; width: 150px;">
                                <i class="bi bi-unlock-fill"></i>
                                Unlock
                            </a>
                            <a href="/admin/user/rolemanagement?id=${data.id}" class="btn btn-danger text-white mx-2" style="cursor: pointer; width: 150px;">
                                <i class="bi bi-pencil-square"></i>
                                Permission
                            </a>
                        </div>
                    `
                    } else {
                        return `
                        <div onclick="lockUnlock('${data.id}')" class="text-center">
                            <a class="btn btn-danger text-white mx-2" style="cursor: pointer; width: 150px;">
                                <i class="bi bi-lock-fill"></i>
                                Lock
                            </a>
                            <a href="/admin/user/rolemanagement?id=${data.id}" class="btn btn-danger text-white mx-2" style="cursor: pointer; width: 150px;">
                                <i class="bi bi-pencil-square"></i>
                                Permission
                            </a>
                        </div>
                    `
                    }

                },
                width: "20%"
            }
        ]
    });
}

function lockUnlock(id) {
    $.ajax({
        type: 'POST',
        url: '/admin/user/lockunlock',
        data: JSON.stringify(id),
        contentType: 'application/json',
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            } else {
                toastr.error(data.message);
            }
        }
    });
}