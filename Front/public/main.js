let clientsDiv
let form
let clients = []

const onClickRestart = (id, machineName) => {
  if (window.confirm(`Are you sure you want to restart ${machineName}?`)) {
    const date = new Date()
    firebase.database().ref(`/clients/${id}/lastRestart`).set(date.toJSON())
    update()
  }
}

const update = () => {
  clients = JSON.parse(localStorage.getItem("clients") ?? "[]")

  clients.forEach((id) => {
    firebase.database().ref(`/clients/${id}`).once("value", (snapshot) => {
      const client = snapshot.val()
      if (!client) return;

      const { lastPing, lastRestart, machineName } = client
      const deltaMinutes = (new Date() - new Date(lastPing)) / 60000
      let status = deltaMinutes > 1 ? "OFFLINE" : "ONLINE"

      const prev = document.getElementById(id)
      if (prev) {
        prev.parentNode.removeChild(prev)
      }

      clientsDiv.innerHTML += `
        <span class="card" id="${id}">
            <div><span>${status === "ONLINE" ? "💚" : "🔴"}</span><b> ${machineName} - ${id} (${status})</b></div>
            <div class="divider"></div>
            Last Ping ${new Date(lastPing).toGMTString()}<br>
            Last Restart ${lastRestart === "never" ? "never" : new Date(lastRestart).toGMTString()}<br>
            <button onclick="onClickRestart('${id}', '${machineName}')"> Restart </button>
        </span>
        `

      if (status === "OFFLINE") {
        clientsDiv.lastElementChild.lastElementChild.remove()
      }

      document.querySelector(".lds-dual-ring").style.display = "none";
    })
  })
}

const onAddClient = (ev) => {
  const newId = ev.target[0].value.toLowerCase()
  if (newId.length !== 6) {
    window.alert("Client ID must be 6 characters long")
    return;
  }
  
  localStorage.setItem("clients", JSON.stringify(Array.from(new Set([...clients, newId]))))
  ev.target[0].value = ""
  update()
}

document.addEventListener('DOMContentLoaded', function () {
  clientsDiv = document.querySelector("#clients")
  form = document.getElementById("form")

  update()
  setInterval(update, 10000)

  form.addEventListener("submit", onAddClient)

  try {
    firebase.app();
    document.getElementById('load').innerHTML = ""
  } catch (e) {
    console.error(e);
    document.getElementById('load').innerHTML = 'Error loading the Firebase SDK, check the console.';
  }
});