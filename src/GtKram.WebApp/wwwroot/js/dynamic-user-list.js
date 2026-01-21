class DynamicUserList {

    constructor(formNameUsers, formNameCheckedUsers, formNamePersons, formNameCheckedPersons) {
        this.formNameUsers = formNameUsers;
        this.formNameCheckedUsers = formNameCheckedUsers;
        this.formNamePersons = formNamePersons;
        this.formNameCheckedPersons = formNameCheckedPersons;
    }

    addItem(inputName, inputChecked, personName, value, iconClass) {
        const panelBlock = `<a class="panel-block"><span class="panel-icon"><i class="${iconClass}"></i></span>${personName}</a>`;
        const input = `<input type="hidden" name="${inputName}" value="${value}" />`;

        if (inputChecked) {
            const col1 = `<div class="column">${panelBlock}</div>`;
            const col2 = `<div class="column is-narrow"><input type="checkbox" class="m-3" name="${inputChecked}" value="${value}" /></div>`;
            const row = `<div class="helper-row columns is-gapless mb-0">${col1}${col2}${input}</div>`;
            $('#user-items').append(row);
        }
        else {
            const row = `<div class="helper-row">${panelBlock}${input}</div>`;
            $('#user-items').append(row);
        }
    }

    addUser(name, id) {
        this.addItem(this.formNameUsers, this.formNameCheckedUsers, name, id, 'fas fa-circle-user');
    }

    addPerson(name) {
        this.addItem(this.formNamePersons, this.formNameCheckedPersons, name, name, 'far fa-circle-user');
    }

    registerClick(selector) {
        $('body').on('click', selector, function () {
            const row = $(this).closest('.helper-row');
            row.remove();
        });
    }

    registerChange(selector) {
        const self = this;
        $(selector).on('change', function () {
            const item = $(this).find(':selected');
            const val = item.val();
            if (val) {
                self.addUser(item.text(), val);
                $(selector).val('').trigger('change');
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