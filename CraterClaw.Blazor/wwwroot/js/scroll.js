window.craterClaw = {
    registerOutsideClick: function (dotNetHelper, containerId) {
        function handler(e) {
            const container = document.getElementById(containerId);
            if (container && container.contains(e.target)) return;
            dotNetHelper.invokeMethodAsync('CloseDropdown');
            document.removeEventListener('click', handler, true);
        }
        setTimeout(function () {
            document.addEventListener('click', handler, true);
        }, 0);
    },
    scrollToBottom: function (id) {
        const el = document.getElementById(id);
        if (!el) return;
        if (!el._ccInit) {
            el._ccInit = true;
            el._userScrolled = false;
            el.addEventListener('scroll', function () {
                el._userScrolled = el.scrollHeight - el.scrollTop - el.clientHeight > 10;
            });
        }
        if (!el._userScrolled) el.scrollTop = el.scrollHeight;
    },
    resetScroll: function (id) {
        const el = document.getElementById(id);
        if (el) el._userScrolled = false;
    }
};
