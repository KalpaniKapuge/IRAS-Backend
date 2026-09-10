"""
Exact reproductions (from the real classification-report numbers supplied)
of the confusion-matrix grid and the full-vs-text-only accuracy comparison.
Every cell below was cross-checked against the precision/recall/support in
the classification reports before being hard-coded here — see the checks in
the module docstring of the accompanying thesis chapter text.
"""
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

plt.rcParams["font.family"] = "Times New Roman"
OUT = os.path.join(os.path.dirname(__file__), "figures")
os.makedirs(OUT, exist_ok=True)

LABELS = ["No Fit", "Potential Fit", "Good Fit"]
MODELS = ["Logistic Regression", "Random Forest", "SVM (linear)", "XGBoost"]

# Exact confusion matrices (full-feature models), rows = actual, cols = predicted.
CONF = {
    "Logistic Regression": [[95, 3, 0], [0, 75, 0], [0, 1, 97]],
    "Random Forest":       [[95, 3, 0], [7, 62, 6], [0, 2, 96]],
    "SVM (linear)":        [[95, 3, 0], [0, 75, 0], [0, 2, 96]],
    "XGBoost":             [[96, 2, 0], [1, 74, 0], [0, 0, 98]],
}

fig, axes = plt.subplots(1, 4, figsize=(19, 4.4))
for ax, model in zip(axes, MODELS):
    cm = np.array(CONF[model])
    im = ax.imshow(cm, cmap="Blues", vmin=0, vmax=98)
    ax.set_title(model, fontsize=12)
    ax.set_xticks(range(3)); ax.set_xticklabels(LABELS, rotation=0)
    ax.set_yticks(range(3)); ax.set_yticklabels(LABELS)
    ax.set_xlabel("Predicted"); ax.set_ylabel("Actual")
    for i in range(3):
        for j in range(3):
            val = cm[i, j]
            color = "white" if val > 55 else "black"
            ax.text(j, i, str(val), ha="center", va="center", color=color, fontsize=11)
fig.tight_layout()
fig.savefig(os.path.join(OUT, "confusion_matrix_chart.png"), dpi=170)
plt.close(fig)

# ---- full vs text-only accuracy (exact) ----
full_acc = {"Logistic Regression": 0.985, "Random Forest": 0.934, "SVM (linear)": 0.982, "XGBoost": 0.989}
text_acc = {"Logistic Regression": 0.587, "Random Forest": 0.627, "SVM (linear)": 0.542, "XGBoost": 0.723}

fig, ax = plt.subplots(figsize=(9, 5.2))
x = np.arange(len(MODELS))
w = 0.32
ax.bar(x - w/2, [full_acc[m] for m in MODELS], width=w, label="full", color="#1f77b4")
ax.bar(x + w/2, [text_acc[m] for m in MODELS], width=w, label="text_only", color="#ff7f0e")
ax.axhline(0.8, color="crimson", linestyle="--", linewidth=1.3, label="80% target")
ax.set_ylabel("Test accuracy")
ax.set_xlabel("model")
ax.set_xticks(x); ax.set_xticklabels(MODELS, rotation=20, ha="right")
ax.set_ylim(0, 1.0)
ax.set_title("Model comparison: full features vs. text-only")
ax.legend(loc="upper right")
fig.tight_layout()
fig.savefig(os.path.join(OUT, "full_vs_text_chart.png"), dpi=170)
plt.close(fig)

print("Exact confusion-matrix and full-vs-text charts regenerated.")
