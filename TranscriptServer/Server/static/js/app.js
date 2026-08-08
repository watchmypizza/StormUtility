document.addEventListener("DOMContentLoaded", () => {
  const prismStyles = document.createElement("link");
  prismStyles.rel = "stylesheet";
  prismStyles.href = "/static/js/node_modules/prismjs/themes/prism-tomorrow.css";
  document.head.appendChild(prismStyles);

  const prismScript = document.createElement("script");
  prismScript.src = "/static/js/node_modules/prismjs/prism.js";
  document.head.appendChild(prismScript);

  prismScript.onload = () => {
    const autoloaderScript = document.createElement("script");
    autoloaderScript.src = "/static/js/node_modules/prismjs/plugins/autoloader/prism-autoloader.js";
    document.head.appendChild(autoloaderScript);

    autoloaderScript.onload = () => {
      Prism.plugins.autoloader.languages_path = "/static/js/node_modules/prismjs/components/";
      const messagesContainer = document.querySelector(".messages");
      if (!messagesContainer) return;

      const messages = messagesContainer.querySelectorAll(".message");

      messages.forEach((message) => {
        const msgContent = message.querySelector(".msg-content");
        if (!msgContent) return;

        let rawText = msgContent.textContent;

        rawText = rawText.replace(
          /```(\w*)\s*([\s\S]*?)```/g,
          (match, lang, code) => {
            const language = lang
              ? lang.toLowerCase().trim()
              : "plaintext";

            const cleanCode = code
              .replace(/^\r?\n/, "")
              .replace(/\r?\n$/, "");

            return `<pre><code class="language-${language}">${DOMPurify.sanitize(cleanCode)
              }</code></pre>`;
          }
        );

        rawText = rawText.replace(
          /`([^`]+)`/g,
          '<code class="language-plaintext">$1</code>'
        );

        msgContent.innerHTML = DOMPurify.sanitize(rawText, {
          ALLOWED_TAGS: [
            "pre",
            "code",
            "span",
            "br",
            "strong",
            "em",
            "u",
            "s"
          ],
          ALLOWED_ATTR: ["class"]
        });
      });

      Prism.highlightAll();
    };
  };
});
