// Shows thumbnails for chosen images and checks count / size before the form is submitted.
// Usage: <input type="file" multiple data-preview="#grid" data-message="#msg" data-max="8" data-max-mb="5">
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
        var grid = document.querySelector(input.dataset.preview);
        var message = input.dataset.message ? document.querySelector(input.dataset.message) : null;
        var max = parseInt(input.dataset.max || '8', 10);
        var maxMb = parseFloat(input.dataset.maxMb || '5');

        function showMessage(text) {
            if (message) message.textContent = text;
        }

        input.addEventListener('change', function () {
            grid.innerHTML = '';
            showMessage('');

            var files = Array.from(input.files);

            if (files.length > max) {
                showMessage('You can upload up to ' + max + ' photos.');
                input.value = '';
                return;
            }

            var tooBig = files.find(function (f) { return f.size > maxMb * 1024 * 1024; });
            if (tooBig) {
                showMessage('"' + tooBig.name + '" is larger than ' + maxMb + ' MB.');
                input.value = '';
                return;
            }

            files.forEach(function (file, index) {
                var tile = document.createElement('div');
                tile.className = 'preview-tile';

                var img = document.createElement('img');
                img.src = URL.createObjectURL(file);
                img.alt = file.name;
                tile.appendChild(img);

                if (index === 0) {
                    var badge = document.createElement('span');
                    badge.className = 'preview-badge';
                    badge.textContent = 'Cover photo';
                    tile.appendChild(badge);
                }

                grid.appendChild(tile);
            });
        });
    });
});