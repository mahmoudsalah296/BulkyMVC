﻿let dataTable;

$(document).ready(function () {
    let url = window.location.search;
    if (url.includes("approved"))
        loadDataTable("approved");
    else if (url.includes("inprocess"))
        loadDataTable("inprocess");
    else if (url.includes("pending"))
        loadDataTable("pending");
    else if (url.includes("completed"))
        loadDataTable("completed");
    else
        loadDataTable("all");
});

function loadDataTable(status) {
    dataTable = $('#tableData').DataTable({
        "ajax": { url: `/admin/order/getall?status=${status}` },
        "columns": [
            { data: 'id', width: "10%" },
            { data: 'name', width: "15%" },
            { data: 'phoneNumber', width: "15%" },
            { data: 'applicationUser.email', width: "20%" },
            { data: 'orderStatus', width: "10%" },
            { data: 'orderTotal', width: "10%" },
            {
                data: 'id',
                'render': function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a href="/admin/order/details?orderId=${data}" class="btn btn-primary text-warning mx-2" asp-route-id="@product.Id">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                        </div>
                    `
                },
                width: "10%"
            }
        ]
    });
}
