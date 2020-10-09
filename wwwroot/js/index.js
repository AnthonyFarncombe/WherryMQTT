"use strict";

const connection = new signalR.HubConnectionBuilder().withUrl("/mqtthub").build();

const app = {
    data: () => ({
        messages: []
    }),
    async mounted() {
        await connection.start();
        connection.on("NewMessage", (topic, payload) => {
            this.messages.push({ topic, payload: JSON.parse(payload) });
        });
    }
};

Vue.createApp(app).mount("#app");
