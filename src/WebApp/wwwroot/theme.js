function changeTheme() {
    const isDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const img = document.getElementById("logo");
    const gradientDark = "radial-gradient(90.68% 40.04% at 80.38% 45.37%, #29dff80f 0%, #4473eb00 100%), radial-gradient(31.2% 31.19% at 49.79% 29.97%, #237bff14 0%, #4473eb00 100%), radial-gradient(circle at 44.03% 100%, #a151ff1a 0%, #ff2fde00 50%), linear-gradient(#232527 0% 100%)";
    const gradientLight = "radial-gradient(circle at 80% 32%,#44ebc314 0%,#7ad8dd14 15%,#4473eb05 40%),radial-gradient(31.2% 31.19% at 49.79% 29.97%,#4473eb14 0%,#4473eb00 100%),radial-gradient(circle at 40% 95%,#8230ff0f 0%,#8230ff00 50%),linear-gradient(#ecf0f799 0% 100%)"
    const el = document.querySelector(".backgradient");
    const elpin = document.querySelector(".backgradient-pin");
    if (isDarkMode) {

        if (el) {
            el.style.background = gradientDark;
            el.classList.add("dark");
            elpin.classList.add("dark");
            document.body.classList.add("dark");
            /*  document.getElementById("overscroll").style.background = gradientDark;*/
            img.src = "images/logofordark.png";
        }


    } else {

        if (el) {
            el.style.background = gradientLight;
            el.classList.remove("dark");
            elpin.classList.remove("dark");
            document.body.classList.remove("dark");
            /*  document.getElementById("overscroll").style.background = gradientLight;*/

            img.src = "images/logoforlight.png";
        }
       
    }
   
}

document.addEventListener("DOMContentLoaded", function () {
    changeTheme();
});