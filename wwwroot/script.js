// al presionar el boton enviar del formulario muestra un alert
// y permite que el formulario se envíe normalmente (para que el servidor haga la redirección)
var form = document.getElementById("miFormulario");
if (form) {
    form.addEventListener("submit", function(event) {
        // mostramos un aviso al usuario
        alert("Formulario enviado");
        // No llamamos a event.preventDefault() para permitir el POST tradicional
    });
}

var queryForm = document.getElementById("queryForm");
if (queryForm) {
    queryForm.addEventListener("submit", function(event) {
        // mostramos un aviso al usuario
        alert("Formulario de Query Parameters enviado");
        // No llamamos a event.preventDefault() para permitir el GET tradicional
    });
}