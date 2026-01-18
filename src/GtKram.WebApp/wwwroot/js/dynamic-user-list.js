class DynamicUserList {

    constructor(formNameUsers, formNamePersons) {
        this.formNameUsers = formNameUsers;
        this.formNamePersons = formNamePersons;
    }

    addItem(inputName, personName, value, iconClass) {
        const panelBlock = `<a class="panel-block"><span class="panel-icon"><i class="${iconClass}"></i></span>${personName}</a>`;
        const container = $('<div>').append(
            $('<input>').attr({ type: 'hidden', name: inputName, value: value }),
            panelBlock);
        $('#user-items').append(container);
    }

    addUser(name, id) {
        this.addItem(this.formNameUsers, name, id, 'fas fa-circle-user');
    }

    addPerson(name) {
        this.addItem(this.formNamePersons, name, name, 'far fa-circle-user');
    }

    registerClick(selector) {
        $('body').on('click', selector, function () {
            $(this).parent().remove();
        });
    }

    registerChange(selector) {
        const self = this;
        $(selector).on('change', function () {
            const item = $(this).find(':selected');
            const val = item.val();
            if (val) {
                self.addUser(item.text(), val);
                $(selector).val('').change();
            }
        });
    }

    registerKeypress(selector) {
        const self = this;
        $(selector).on('keypress', function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                e.stopPropagation();
                const v = $(this).val()?.trim();
                if (v?.length) {
                    $(this).val('');
                    self.addPerson(v);
                }
            }
        });
    }
}