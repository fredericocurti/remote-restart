let clientsDiv

const onClickBtn = (client) => {
    const date = new Date()
    firebase.database().ref(`/clients/${client}/lastRestart`).set(date.toJSON())
    update()
}

const update = () => {
    firebase.database().ref('/clients').once('value', snapshot => {
        clientsDiv.innerHTML = ""
        Object.entries(snapshot.val()).map(([client, val]) => {
            const { lastPing, lastRestart } = val

            const deltaMinutes = (new Date() - new Date(lastPing)) / 60000

            let status = "ONLINE"
            if (deltaMinutes > 1) {
                status = "OFFLINE"
            }

            clientsDiv.innerHTML += `
            <span class="card">
                <div><b>${status === "ONLINE" ? "💚" : "🔴"} ${client} (${status})</b></div>
                <div class="divider"></div>
                Last Ping ${new Date(lastPing)}<br>
                Last Restart ${lastRestart === "never" ? "never" : new Date(lastRestart)}<br>
                <button onclick="onClickBtn('${client}')"> Restart </button>
            </span>
            `
            if (status === "OFFLINE") {
                clientsDiv.lastElementChild.lastElementChild.remove()
            }
        })
    })
}

document.addEventListener('DOMContentLoaded', function () {
    clientsDiv = document.querySelector("#clients")
    // // The Firebase SDK is initialized and available here!
    update()
    setInterval(update, 5000)
    document.querySelectorAll("button").forEach(b => b.addEventListener("click", (ev) => { }))

    try {
        let app = firebase.app();
        let features = ['auth', 'database', 'messaging', 'storage'].filter(feature => typeof app[feature] === 'function');
        // document.getElementById('load').innerHTML = `Firebase SDK loaded with ${features.join(', ')}`;
        document.getElementById('load').innerHTML = ""

    } catch (e) {
        console.error(e);
        document.getElementById('load').innerHTML = 'Error loading the Firebase SDK, check the console.';
    }
});