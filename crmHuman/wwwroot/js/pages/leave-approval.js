function approveLeave(id, status) {
    $('#approveId').val(id);
    $('#approveStatus').val(status);
    $('#approveComment').val('');
    $('#commentModal').modal('show');
}

async function submitApproval() {
    const data = {
        Id: parseInt($('#approveId').val()),
        Status: parseInt($('#approveStatus').val()),
        Comment: $('#approveComment').val()
    };

    try {
        const response = await fetch('?handler=Approve', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        if (result.success) {
            alert('Thao tác thành công');
            location.reload();
        } else {
            alert(result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Lỗi hệ thống');
    }
}
