export function exactQuiz({ inputId, buttonId, feedbackId, answer, success, retry }) {
  const input = document.getElementById(inputId);
  const feedback = document.getElementById(feedbackId);
  document.getElementById(buttonId).addEventListener("click", () => {
    const correct = input.value.trim().toLowerCase() === answer.toLowerCase();
    feedback.textContent = correct ? success : retry;
    feedback.style.color = correct ? "#087f5b" : "#b42318";
  });
}
