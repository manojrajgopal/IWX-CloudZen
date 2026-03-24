from flask import Flask

app = Flask(__name__)

@app.route("/")
def home():
    return "IWX CloudZen Python Development Running"

@app.route("/health")
def health():
    return {
        "status" : "Running...",
        "service" : "Python deployment"
    }

if __name__ == "__main__":
    app.run(host = "0.0.0.0", port = 5000)
