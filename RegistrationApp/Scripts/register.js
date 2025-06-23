$(document).ready(function () {
    // Submit form via AJAX
    $('#regForm').submit(function (e) {
        e.preventDefault();

        $.ajax({
            url: '/User/Register', // Replaced @Url.Action
            method: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.status === 'success') {
                    alert("User Registered!");
                    $('#regForm')[0].reset();
                    loadUsers();

                    // Redirect to login page
                    window.location.href = res.redirectUrl;
                } else {
                    alert("Registration failed.");
                    alert(res.message || "CAPTCHA or form validation failed.");
                    refreshCaptcha(); //refresh captcha on failure
                }
            },
            error: function () {
                alert("Something went wrong.");
            }
        });
    });

    // Load users list
    function loadUsers() {
        $.get('/User/GetUsers', function (users) { // Replaced @Url.Action
            let html = '';
            users.forEach(u => {
                html += `<li>${u.Name} (${u.UserName}) - ${u.Email}, Age: ${u.Age}, Gender: ${u.Gender}</li>`;
            });
            $('#userList').html(html);
        });
    }

    loadUsers();
});

function refreshCaptcha() {
    $.get('/User/GetCaptchaText', function (res) {
        $('#captchaText').text(res.captcha);
    });
}