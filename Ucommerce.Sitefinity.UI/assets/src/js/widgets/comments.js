import { initializeComponent } from "../functions/init";

initializeComponent("comments", initCart);

function initCart(rootElement) {
    new Vue({
        el: '#' + rootElement.id,
        data: {
            model: null
        },
        methods: {
            fetchData: function () {
            this.$http.get('', {}).then((response) => {
                if (response.data) {
                    this.model = response.data;
                }
            });
            }
        },
        created: function () {
            this.fetchData();
        }
    });
}




