document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('ttsForm');
    const textInput = document.getElementById('textInput');
    const convertBtn = document.getElementById('convertBtn');
    const btnText = document.getElementById('btnText');
    const btnLoader = document.getElementById('btnLoader');
    const resultDiv = document.getElementById('result');
    const errorDiv = document.getElementById('error');
    const audioPlayer = document.getElementById('audioPlayer');
    const downloadBtn = document.getElementById('downloadBtn');
    const charCount = document.getElementById('charCount');

    let currentAudioUrl = null;
    let lastRequestTime = 0;
    const MIN_REQUEST_INTERVAL = 3000; // 3 seconds between requests
    const MAX_TEXT_LENGTH = 1000;

    // Update character count
    textInput.addEventListener('input', () => {
        const length = textInput.value.length;
        charCount.textContent = `${length}/${MAX_TEXT_LENGTH}`;

        if (length > MAX_TEXT_LENGTH) {
            charCount.style.color = '#dc2626';
        } else if (length > MAX_TEXT_LENGTH * 0.9) {
            charCount.style.color = '#f59e0b';
        } else {
            charCount.style.color = '#6b7280';
        }
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const text = textInput.value.trim();
        const voice = document.getElementById('voiceSelect').value;

        console.log('Selected voice:', voice);

        // Validation
        if (!text) {
            showError('Please enter some text to convert.');
            return;
        }

        if (text.length > MAX_TEXT_LENGTH) {
            showError(`Text is too long. Maximum ${MAX_TEXT_LENGTH} characters allowed.`);
            return;
        }

        // Client-side throttling
        const now = Date.now();
        const timeSinceLastRequest = now - lastRequestTime;

        if (timeSinceLastRequest < MIN_REQUEST_INTERVAL) {
            const waitTime = Math.ceil((MIN_REQUEST_INTERVAL - timeSinceLastRequest) / 1000);
            showError(`Please wait ${waitTime} second(s) before making another request.`);
            return;
        }

        // Hide previous results/errors
        hideError();
        hideResult();

        // Show loading state
        setLoading(true);
        lastRequestTime = now;

        try {
            const requestBody = { text: text, voice: voice };
            console.log('Sending request:', requestBody);

            const response = await fetch('/api/tts/convert', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestBody)
            });

            // Handle rate limiting
            if (response.status === 429) {
                throw new Error('Rate limit exceeded. Please wait a minute before trying again.');
            }

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || `Server error: ${response.status}`);
            }

            // Get the audio blob from the response
            const audioBlob = await response.blob();
            const audioUrl = URL.createObjectURL(audioBlob);

            // Store the URL for download
            currentAudioUrl = audioUrl;

            // Set the audio source and show the result
            audioPlayer.src = audioUrl;
            showResult();

        } catch (error) {
            console.error('Error:', error);
            showError(`${error.message}`);
            // Reset the timer if request failed
            lastRequestTime = 0;
        } finally {
            setLoading(false);
        }
    });

    function setLoading(isLoading) {
        convertBtn.disabled = isLoading;
        if (isLoading) {
            btnText.classList.add('hidden');
            btnLoader.classList.remove('hidden');
        } else {
            btnText.classList.remove('hidden');
            btnLoader.classList.add('hidden');
        }
    }

    function showResult() {
        resultDiv.classList.remove('hidden');
    }

    function hideResult() {
        resultDiv.classList.add('hidden');
    }

    function showError(message) {
        errorDiv.textContent = message;
        errorDiv.classList.remove('hidden');
    }

    function hideError() {
        errorDiv.classList.add('hidden');
    }

    // Download button click handler
    downloadBtn.addEventListener('click', () => {
        if (currentAudioUrl) {
            const link = document.createElement('a');
            link.href = currentAudioUrl;
            link.download = 'speech.mp3';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }
    });
});
