function createToast(text, colorClass = 'is-primary') {
    const id = '_' + Math.random().toString(36).substr(2, 9);
    const html = `<div class="notification ${colorClass} has-fadein" id="${id}"><button class="delete"></button>${text}</div>`;
    $(".toast-container").append(html);
    const element = $(`#${id}`);
    setTimeout(function () {
        element.fadeOut();
        element.remove();
    }, 5000);
};

function openModal(event) {
    event.stopPropagation();
    event.preventDefault();
    const id = event.currentTarget.dataset.target;
    const modal = $(id);
    modal.toggleClass('is-active');
    $('html').toggleClass('is-clipped');
    const openEvent = jQuery.Event("modal:open");
    openEvent.relatedTarget = event.currentTarget;
    modal.trigger(openEvent);
};

function closeModal(event) {
    event.stopPropagation();
    const modal = $(this).closest(".modal");
    modal.toggleClass('is-active');
    $('html').toggleClass('is-clipped');
    modal.trigger("modal:close");
}

function handleModal(args) {
    const defaults = {
        id: '',
        token: '',
        init: function (target, relatedTarget) { },
        load: {
            dataurl: '',
            action: function (target, data) { },
            toast: { failed: 'Fehler' }
        },
        confirm: {
            dataurl: '',
            action: function () { },
            toast: { success: 'OK', failed: 'Fehler' },
            closeOnFailed: false
        },
    };
    const params = { ...defaults, ...args };
    const modal = $(params.id);
    modal.on('modal:open', function (e) {
        params.init($(e.target), e.relatedTarget);

        const confirm = $(e.target).find(".confirm");
        const loading = $(e.target).find('.loading-value');
        const close = $(e.target).find(".close-modal").get(0);

        if (params.load.dataurl) {
            loading.removeClass('is-hidden');
            const url = e.relatedTarget.dataset[params.load.dataurl];
            $.post(url, params.token).done(function (data) {
                if (data) {
                    loading.addClass('is-hidden');
                    params.load.action($(e.target), data);
                    confirm.attr("disabled", false);
                } else {
                    createToast(params.load.toast.failed, 'is-danger');
                }
            });
        } else {
            confirm.attr("disabled", false);
        }

        const url = e.relatedTarget.dataset[params.confirm.dataurl];
        confirm.click(function (e) {
            e.preventDefault();
            confirm.addClass("is-loading");
            $.post(url, params.token).done(function (data) {
                if (data) {
                    createToast(params.confirm.toast.success);
                    params.confirm.action();
                } else {
                    createToast(params.confirm.toast.failed, 'is-danger');
                    if (params.confirm.closeOnFailed) {
                        close.click();
                    }
                }
            });
        });
    });
    modal.on('modal:close', function (e) {
        $(e.target).find('.loading-value').addClass('is-hidden');
        if (params.init.datainfo) {
            $(e.target).find(params.init.selector).text('');
        }
        if (params.load.dataurl) {
            $(e.target).find(params.load.selector).text('');
        }
        const confirm = $(e.target).find(".confirm");
        confirm.attr("disabled", true);
        confirm.removeClass("is-loading");
        confirm.off();
    });
}

function sleep(time) {
    return new Promise((resolve) => setTimeout(resolve, time));
}

$(function () {
    $(".navbar-burger").click(function () {
        $(".navbar-burger").toggleClass("is-active");
        $(".navbar-menu").toggleClass("is-active");
    });

    // should work for dynamic created elements also
    $("body").on("click", ".notification > button.delete", function () {
        $(this).parent().addClass('is-hidden').remove();
        return false;
    });

    $(".copy-link").click(function (e) {
        e.stopPropagation();
        e.preventDefault();
        const url = $(this).data('url');
        const tempInput = $("<input>");
        $("body").append(tempInput);
        tempInput.val(url).select();
        document.execCommand("copy");
        tempInput.remove();
        createToast("Link wurde in die Zwischenablage kopiert.");
    });

    $('.open-modal').click(openModal);
    $('.close-modal').click(closeModal);

    $('.is-toggle-password').click(function () {
        const input = $(this).parent().parent().find('input');
        const isPassword = input.attr('type') === 'password';
        input.attr('type', isPassword ? 'text' : 'password');
    });

    $(".list-item-clickable").on('click', function (e) {
        e.stopPropagation();
        e.preventDefault();
        window.location = $(this).data("url");
    });
});