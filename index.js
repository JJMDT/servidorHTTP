const express = require("express");
const axios = require("axios"); // Vamos a usar axios para hacer peticiones a Meta (instalalo con `npm install axios` si no lo tenés)
const app = express();

app.use(express.json());

const PORT = process.env.PORT || 3000;
const VERIFY_TOKEN = "micodigosecreto123";

// Token temporal que te da Meta en el panel de pruebas (¡cámbialo si generas uno nuevo!)
const WHATSAPP_TOKEN = "TU_TOKEN_DE_ACCESO_TEMPORAL_DE_META";
const PHONE_NUMBER_ID = "TU_PHONE_NUMBER_ID_DE_META"; // El ID del número de prueba

// 1. VERIFICACIÓN DEL WEBHOOK (GET)
app.get("/webhook", (req, res) => {
  const mode = req.query["hub.mode"];
  const token = req.query["hub.verify_token"];
  const challenge = req.query["hub.challenge"];

  if (mode && token === VERIFY_TOKEN) {
    console.log("¡WEBHOOK_VERIFIED con éxito!");
    res.status(200).send(challenge);
  } else {
    res.sendStatus(403);
  }
});

// 2. RECIBIR Y RESPONDER MENSAJES (POST)
app.post("/webhook", async (req, res) => {
  const body = req.body;

  if (body.object) {
    const entry = body.entry?.[0];
    const changes = entry?.changes?.[0];
    const value = changes?.value;
    const message = value?.messages?.[0];

    if (message) {
      const numeroRemitente = message.from; // Ej: 54911XXXXXXXX
      const textoMensaje = message.text?.body;

      console.log(`Mensaje de ${numeroRemitente}: "${textoMensaje}"`);

      // RESPONDER AUTOMÁTICAMENTE A WHATSAPP
      try {
        await axios.post(
          `https://graph.facebook.com/v18.0/${PHONE_NUMBER_ID}/messages`,
          {
            messaging_product: "whatsapp",
            to: numeroRemitente,
            text: {
              body: `¡Hola! Recibí tu mensaje: "${textoMensaje}". Pronto te responderá la IA 🚀`,
            },
          },
          {
            headers: {
              Authorization: `Bearer ${WHATSAPP_TOKEN}`,
              "Content-Type": "application/json",
            },
          },
        );
        console.log("¡Respuesta enviada con éxito al celular!");
      } catch (error) {
        console.error(
          "Error al enviar mensaje a WhatsApp:",
          error.response?.data || error.message,
        );
      }
    }

    res.sendStatus(200);
  } else {
    res.sendStatus(404);
  }
});

app.listen(PORT, () => {
  console.log(`Servidor corriendo en el puerto ${PORT}`);
});
