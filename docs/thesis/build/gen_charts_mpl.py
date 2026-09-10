"""Data charts (matplotlib) built from the real figures reported in the
Interim Submission 02 report (Table 4.2, 4.3, 4.4)."""
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

plt.rcParams["font.family"] = "Times New Roman"
OUT = os.path.join(os.path.dirname(__file__), "figures")
os.makedirs(OUT, exist_ok=True)

# ---- Chart 1: per-class recall by model (derived from Table 4.4 diagonal counts / test totals) ----
models = ["Logistic\nRegression", "Random\nForest", "SVM\n(Linear)", "XGBoost"]
totals = {"No Fit": 98, "Potential Fit": 75, "Good Fit": 98}
correct = {
    "No Fit":        [95, 95, 95, 96],
    "Potential Fit":  [75, 62, 75, 74],
    "Good Fit":      [97, 96, 96, 98],
}
recall = {k: [c / totals[k] * 100 for c in v] for k, v in correct.items()}

fig, ax = plt.subplots(figsize=(9, 5))
x = np.arange(len(models))
w = 0.25
colors = ["#2554C7", "#F2A93B", "#3FA34D"]
for i, (cls, vals) in enumerate(recall.items()):
    ax.bar(x + (i - 1) * w, vals, width=w, label=cls, color=colors[i])
ax.set_ylabel("Per-class recall on test set (%)")
ax.set_xticks(x)
ax.set_xticklabels(models)
ax.set_ylim(0, 110)
ax.axhline(80, color="crimson", linestyle="--", linewidth=1, label="80% target")
ax.legend(loc="lower right", fontsize=9)
ax.set_title("Per-Class Recall by Model (full-feature configuration)")
for spine in ("top", "right"):
    ax.spines[spine].set_visible(False)
fig.tight_layout()
fig.savefig(os.path.join(OUT, "confusion_matrix_chart.png"), dpi=170)
plt.close(fig)

# ---- Chart 2: full-feature vs text-only accuracy (Table 4.2 vs Table 4.3) ----
full_acc = [98.5, 93.4, 98.2, 98.9]
text_acc = [58.7, 62.7, 54.2, 72.3]

fig, ax = plt.subplots(figsize=(9, 5))
x = np.arange(len(models))
w = 0.32
ax.bar(x - w/2, full_acc, width=w, label="Full-feature model", color="#2554C7")
ax.bar(x + w/2, text_acc, width=w, label="Text-only model", color="#F2A93B")
ax.axhline(80, color="crimson", linestyle="--", linewidth=1, label="80% target")
ax.set_ylabel("Test accuracy (%)")
ax.set_xticks(x)
ax.set_xticklabels(models)
ax.set_ylim(0, 110)
ax.set_title("Full-Feature vs Text-Only Model Accuracy")
ax.legend(loc="upper right", fontsize=9)
for spine in ("top", "right"):
    ax.spines[spine].set_visible(False)
fig.tight_layout()
fig.savefig(os.path.join(OUT, "full_vs_text_chart.png"), dpi=170)
plt.close(fig)

print("charts done")
