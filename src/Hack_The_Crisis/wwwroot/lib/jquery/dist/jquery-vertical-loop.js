/**
 * date: 2017/6/28
 * desc:
 */


(function (root, factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD
        define(['jquery'], factory);
    } else if (typeof exports === 'object') {
        // Node, CommonJS之类的
        module.exports = factory(require('jquery'));
    } else {
        // 浏览器全局变量(root 即 window)
        root.VerticalLoop = factory(root.jQuery);
    }
}(this, function ($) {

    //
    var VerticalLoop = function (element, options) {
        this.opt = $.extend({
            delay: 5000,
            order: 'asc',
            oninitend: function (c) {
            }
        }, options);
        this.container = $(element);
        this.items = this.container.find("li");
        this.index = 0;
        this.pager = null;
        this.animating = false;
        this.screen = false;
        this.mouseIn = false;
        this._init();
    };

    //
    VerticalLoop.prototype = {
        Constructor: VerticalLoop,
        _init: function () {
            var self = this;
            var cloneData = this.container.find("ul").clone();
            if (this.opt.order === 'asc') {
                cloneData.appendTo(this.container);
            } else if (this.opt.order === 'desc') {
                cloneData.prependTo(this.container);
                //
                var ulHeight = cloneData.height();
                cloneData.css('marginTop', '-' + ulHeight + 'px');
            }
            this._to(0);
            this.container.on("mouseenter mousemove", function () {
                self.mouseIn = true;
                self.autoPause();
            });
            this.container.on("mouseleave", function () {
                self.mouseIn = false;
                if (self._isInScreen()) {
                    self.autoStart();
                }
            });
            if (this._isInScreen()) {
                self.autoStart();
                self.screen = true;
            }
            $(window).scroll(function () {
                if (self._isInScreen() && self.screen === false) {
                    self.screen = true;
                    self.autoStart();
                } else {
                    if (!self._isInScreen() && self.screen === true) {
                        self.screen = false;
                        self.autoPause();
                    }
                }
            });
            this.opt.oninitend.call(this, this.container);
        },
        autoStart: function () {
            var self = this;
            this.timer = setInterval(function () {
                self._next();
            }, this.opt.delay);
        },
        autoPause: function () {
            clearInterval(this.timer);
        },
        _next: function () {
            this._to(this.index);
        },
        _to: function (index) {
            var ul = this.container.find("ul").eq(0)
                , containHeight = this.container.height()
                , f = ul.find("li").length // li 总个数
                , self = this;
            //
            var ulHeight = ul.height();
            //
            var liHeight = ul.find("li").height();
            // 必须得被整除掉
            var liShowSize = (parseInt(containHeight / liHeight, 10)) || 1;
            // 总共的翻页次数
            var pageSize = parseInt(f / liShowSize, 10) || 1;
            //
            var offset = 0 - (index * containHeight);

            if (this.opt.order === 'asc') {
                offset = offset;
            } else if (this.opt.order === 'desc') {
                offset = 0 - (ulHeight + offset);
            }


            ul.animate({
                marginTop: offset + "px"
            }, 1000, function () {
                if (index === pageSize) {
                    if (self.opt.order === 'asc') {
                        ul.css('marginTop', 0);
                    } else if (self.opt.order === 'desc') {
                        ul.css('marginTop', 0 - ulHeight);
                    }


                    self.index = 1;
                } else {
                    self.index = index + 1;
                }
            });
        },
        _isInScreen: function () {
            var a = this.container;
            if (a.length > 0) {
                return ($(document).scrollTop() + $(window).height() > a.offset().top) && (a.offset().top + a.height() > $(document).scrollTop());
            }
        }
    };

    // jquery  plugin
    $.fn.verticalLoop = function (option) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('snVerticalLoop');
            var options = {};
            var action = '';
            // 如果参数是 object
            if (typeof option === 'object') {
                options = option;
            }

            //
            if (typeof option === 'string') {
                action = option;
            }

            if (!data) {
                data = new VerticalLoop($this, options);
                $this.data('snVerticalLoop', data);
            }

            if (action) {
                data[action]();
            }
        });
    };

    // 构造函数
    $.fn.verticalLoop.Constructor = VerticalLoop;

    return VerticalLoop;

}));

