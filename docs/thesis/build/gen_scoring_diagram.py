import os
from diagram_helpers import new_canvas, box, arrow, label, load_font, save, INK, ACCENT, ACCENT_FILL, WHITE, GREY_FILL

OUT = os.path.join(os.path.dirname(__file__), "figures")


def d_scoring_pipeline():
    W, H = 1400, 620
    img, dr = new_canvas(W, H)
    f_title = load_font(19, bold=True)
    dr.text((W/2 - dr.textlength("Weighted Scoring Pipeline (ScoringService.ComputeTotalScore)", font=f_title)/2, 12),
            "Weighted Scoring Pipeline (ScoringService.ComputeTotalScore)", font=f_title, fill=INK)

    signals = [
        ("Skill Match", "taxonomy overlap,\nlocally computed", "x 0.6", ACCENT_FILL),
        ("Semantic Similarity", "AI service /rank\n(resume vs job text)", "x 0.4", ACCENT_FILL),
        ("ML Fit Score", "trained Good-Fit\nclassifier probability", "x 0.0*", GREY_FILL),
        ("Assessment Score", "graded skill\nassessment, if set", "x 0.0*", GREY_FILL),
    ]
    x = 60
    w = 280
    y = 110
    for i, (name, sub, weight, fill) in enumerate(signals):
        box(dr, x, y, w, 110, title=name, subtitle=sub, fill=fill,
            title_font=load_font(14, bold=True), sub_font=load_font(11))
        label(dr, x + w/2, y + 128, weight, font=load_font(12, bold=True))
        arrow(dr, (x + w/2, y + 145), (x + w/2, 330), color=ACCENT, width=2)
        x += w + 30

    box(dr, 380, 330, 640, 90, title="Total Score = Σ (weight_i × signal_i)",
        subtitle="Weights configured via ScoringOptions; must sum to 1",
        fill=ACCENT_FILL, title_font=load_font(15, bold=True), sub_font=load_font(11))
    arrow(dr, (700, 420), (700, 470), color=ACCENT, width=2)
    box(dr, 480, 470, 440, 90, title="JobMatch.MatchScore / Application.TotalScore",
        subtitle="Persisted, compared against AutoMatchThreshold (default 0.5)",
        fill=WHITE, title_font=load_font(13, bold=True), sub_font=load_font(10.5))

    f_note = load_font(11)
    dr.text((60, 560), "* MlFitScoreWeight and AssessmentScoreWeight default to 0 so existing deployments keep prior behaviour",
            font=f_note, fill=(90, 90, 90))
    dr.text((60, 578), "  until an operator deliberately opts in; both signals are still computed and stored for later analysis.",
            font=f_note, fill=(90, 90, 90))

    save(img, os.path.join(OUT, "scoring_pipeline.png"))


if __name__ == "__main__":
    d_scoring_pipeline()
