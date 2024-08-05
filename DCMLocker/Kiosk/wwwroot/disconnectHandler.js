window.addEventListener('offline', () => {
    window.location.href = '/';
});

window.addEventListener('online', () => {
    window.location.reload();
});