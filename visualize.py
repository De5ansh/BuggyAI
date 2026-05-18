import matplotlib.pyplot as plt
from collections import Counter

# -------- SETTINGS --------
GRID_SIZE = 50   # adjust if your grid changes
SHOW_HEATMAP = True  # True = intensity heatmap, False = simple scatter

# -------- DATA STORAGE --------
visited = []
bugs = []

plt.gca().set_facecolor("black")

# -------- READ FILE --------
with open("coverage.txt", "r") as f:
    for line in f:
        parts = line.strip().split(",")

        if len(parts) < 3:
            continue

        x = int(parts[0])
        y = int(parts[1])
        tag = parts[2]

        if tag == "visited":
            visited.append((x, y))
        elif tag == "bug":
            bugs.append((x, y))

# -------- DEBUG --------
print(f"Visited cells: {len(visited)}")
print(f"Bug cells: {len(bugs)}")

# -------- PLOTTING --------
plt.figure(figsize=(8, 8))

if SHOW_HEATMAP:
    # 🔥 Heatmap mode (intensity)
    counts = Counter(visited)

    x = [k[0] for k in counts]
    y = [k[1] for k in counts]
    c = [counts[k] for k in counts]

    scatter = plt.scatter(x, y, c=c, cmap="viridis", s=30)
    plt.colorbar(scatter, label="Visit Frequency")

else:
    # ✅ Simple mode
    if visited:
        vx = [v[0] for v in visited]
        vy = [v[1] for v in visited]
        plt.scatter(vx, vy, c="green", s=20, label="Visited")

# 🔴 Plot bugs
if bugs:
    bx = [b[0] for b in bugs]
    by = [b[1] for b in bugs]
    plt.scatter(bx, by, c="red", s=40, label="Bugs")

# -------- GRID SETTINGS --------
plt.xlim(0, GRID_SIZE)
plt.ylim(0, GRID_SIZE)

# 🔥 VERY IMPORTANT: match Unity orientation
plt.gca().invert_yaxis()

# -------- STYLING --------
plt.title("Exploration Heatmap with Bug Locations")
plt.xlabel("X Grid")
plt.ylabel("Z Grid")
plt.legend()
plt.grid(True)

# Optional dark theme (uncomment if you want 🔥 look)
# plt.gca().set_facecolor("black")

plt.show()