/* =================================================
   Seri Döviz UI — menü, efektler, yardımcılar (güncel)
   ================================================= */
(function () {
    const html = document.documentElement;
    const storageKey = 'seridoviz.theme';

    // Navbar scroll + toTop
    function handleScroll() {
        const nav = document.querySelector('.sg-navbar');
        const toTop = document.getElementById('toTop');
        const y = window.scrollY || document.documentElement.scrollTop;
        if (nav) nav.classList.toggle('scrolled', y > 12);
        if (toTop) toTop.classList.toggle('show', y > 250);
    }

    // Aktif menü (daha sağlam)
    // Aktif menü (path + hash bilinci, hem üst hem alt menü)
    function markActiveNav() {
        const norm = (p) => {
            p = (p || '').replace(/\/+$/, '').toLowerCase();
            if (p === '') p = '/';
            // /Home/Index'i köke eşitle
            if (p === '/home' || p === '/home/index') return '/';
            return p;
        };

        const here = new URL(location.href);
        const herePath = norm(here.pathname);
        const hereHash = (here.hash || '').toLowerCase(); // ör. "#about"

        // Hem desktop üst menü hem mobil alt menü
        const links = document.querySelectorAll('.sg-nav .nav-link, .sg-bottomnav .nav-link');

        links.forEach(a => {
            let active = false;
            try {
                const url = new URL(a.href, location.origin);
                const linkPath = norm(url.pathname);
                const linkHash = (url.hash || '').toLowerCase();

                if (linkPath === herePath) {
                    // Sayfalar aynıysa: hash varsa sadece aynı hash'li link aktif
                    // Hash yoksa yalnızca hash'siz link aktif
                    active = hereHash ? (linkHash === hereHash) : !linkHash;
                } else {
                    active = false;
                }
            } catch (_) { /* boş */ }

            a.classList.toggle('active', active);
        });
    }


    // Tooltips
    function initTooltips() {
        if (!window.bootstrap) return;
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => new bootstrap.Tooltip(el));
    }

    // Ripple
    function initRipple() {
        document.body.addEventListener('pointerdown', e => {
            const t = e.target.closest('[data-ripple]');
            if (!t) return;
            const r = document.createElement('span');
            r.className = 'sg-ripple';
            const rect = t.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            r.style.width = r.style.height = size + 'px';
            r.style.left = (e.clientX - rect.left - size / 2) + 'px';
            r.style.top = (e.clientY - rect.top - size / 2) + 'px';
            t.appendChild(r);
            setTimeout(() => r.remove(), 520);
        });
    }

    // Scroll to top
    function wireToTop() {
        const btn = document.getElementById('toTop');
        if (!btn) return;
        btn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
    }

    // Tema butonu
    function wireThemeButton() {
        const btn = document.getElementById('themeBtn');
        const icon = document.getElementById('themeIcon');
        const text = document.getElementById('themeText');
        if (!btn || !icon || !text) return;

        const applyUi = () => {
            const t = html.getAttribute('data-theme') || 'seridoviz-dark';
            if (t === 'seridoviz-dark') { icon.className = 'bi bi-moon-stars'; text.textContent = 'Koyu'; }
            else { icon.className = 'bi bi-brightness-high'; text.textContent = 'Açık'; }
        };

        btn.addEventListener('click', () => {
            const current = html.getAttribute('data-theme') || 'seridoviz-dark';
            const next = current === 'seridoviz-dark' ? 'seridoviz-light' : 'seridoviz-dark';
            html.setAttribute('data-theme', next);
            try { localStorage.setItem(storageKey, next); } catch (_) { }
            applyUi();
        });

        applyUi();
    }

    // Mobil: linke tıklayınca menü kapansın
    function closeNavOnClick() {
        const nav = document.getElementById('mainNav');
        if (!nav || !window.bootstrap) return;
        const bs = new bootstrap.Collapse(nav, { toggle: false });
        nav.querySelectorAll('a.nav-link').forEach(a => a.addEventListener('click', () => bs.hide()));
    }

    // Smooth anchor scroll
    function anchorScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(a => {
            a.addEventListener('click', e => {
                const id = a.getAttribute('href');
                const el = document.querySelector(id);
                if (!el) return;
                e.preventDefault();
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
                history.replaceState(null, '', id);
            });
        });
    }

    // Basit sayfa yükleyici API (+ dönen metinler + MAX süre)
    const PageLoader = (() => {
        const el = document.getElementById('pageLoader');
        const text = el ? el.querySelector('#pageLoaderText') : null;

        // İstersen burayı düzenle
        let msgs = [
            'Güncel kurlar yükleniyor…',
            'Veri alınıyor…',
            'Piyasa akışı hazırlanıyor…',
            'Anlık fiyatlar geliyor…',
            'Bağlantı kuruluyor…',
            'Canlı yayın başlıyor…'
        ];

        let i = 0, rotTimer = null;
        let maxTimer = null;
        let defaultMaxMs = 3000; // ⬅️ maksimum görünme süresi (3 sn)

        const render = () => { if (text) text.textContent = msgs[i % msgs.length]; i++; };

        // show: PageLoader.show(), PageLoader.show(5000) veya PageLoader.show({max:5000, messages:[...]})
        const show = (opts) => {
            if (!el) return;

            // Opsiyonel parametreler
            let maxMs = defaultMaxMs;
            if (typeof opts === 'number') maxMs = opts;
            else if (opts && typeof opts === 'object') {
                if (Array.isArray(opts.messages) && opts.messages.length) { msgs = opts.messages.slice(); i = 0; }
                if (Number.isFinite(opts.max)) maxMs = opts.max;
            }

            // UI
            el.hidden = false;
            el.classList.add('show');

            // Dönen metinler
            i = 0;
            render();
            clearInterval(rotTimer);
            rotTimer = setInterval(render, 1400);

            // MAX süre
            clearTimeout(maxTimer);
            maxTimer = setTimeout(hide, Math.max(0, maxMs));
        };

        const hide = () => {
            if (!el) return;
            el.classList.remove('show');
            setTimeout(() => { el.hidden = true; }, 180);
            clearInterval(rotTimer); rotTimer = null;
            clearTimeout(maxTimer); maxTimer = null;
        };

        const setMessages = (arr) => {
            if (Array.isArray(arr) && arr.length) { msgs = arr.slice(); i = 0; render(); }
        };

        const setMax = (ms) => {
            if (Number.isFinite(ms) && ms >= 0) defaultMaxMs = ms;
        };

        return { show, hide, setMessages, setMax };
    })();
    window.PageLoader = PageLoader;


    // İlk tema tercih
    function initPreferredTheme() {
        try {
            const saved = localStorage.getItem(storageKey);
            if (saved) html.setAttribute('data-theme', saved);
            else html.setAttribute('data-theme', matchMedia('(prefers-color-scheme: dark)').matches ? 'seridoviz-dark' : 'seridoviz-light');
        } catch (_) { }
    }

    // Reveal animasyonu
    function initReveal() {
        const els = [...document.querySelectorAll('.sg-card, .reveal, [data-glow]')];
        if (!('IntersectionObserver' in window)) { els.forEach(e => e.classList.add('show')); return; }
        const io = new IntersectionObserver(entries => {
            entries.forEach(en => {
                if (en.isIntersecting) { en.target.classList.add('show'); io.unobserve(en.target); }
            });
        }, { threshold: .12 });
        els.forEach(e => { e.classList.add('reveal'); io.observe(e); });
    }

    // Tilt efekti
    function initTilt() {
        const els = document.querySelectorAll('.tilt');
        els.forEach(el => {
            let rect;
            const onMove = (e) => {
                rect = rect || el.getBoundingClientRect();
                const cx = rect.left + rect.width / 2;
                const cy = rect.top + rect.height / 2;
                const dx = (e.clientX - cx) / rect.width;
                const dy = (e.clientY - cy) / rect.height;
                el.style.transform = `rotateY(${dx * 6}deg) rotateX(${-dy * 6}deg) translateZ(0)`;
            };
            const reset = () => { el.style.transform = ''; rect = null; };
            el.addEventListener('pointermove', onMove);
            el.addEventListener('pointerleave', reset);
        });
    }

    // Ambient ($ € ₺ Au) üret
    function initAmbient() {
        const host = document.querySelector('.sg-ambient');
        if (!host) return;
        const icons = ['$', '€', '₺', 'Au', '¥', '£'];
        for (let i = 0; i < 14; i++) {
            const s = document.createElement('span');
            s.className = 'float';
            s.textContent = icons[i % icons.length];
            s.style.left = Math.random() * 100 + 'vw';
            s.style.setProperty('--x', (Math.random() * 8 - 4) + 'vw');
            s.style.setProperty('--t', (14 + Math.random() * 16) + 's');
            s.style.animationDelay = (-Math.random() * 20) + 's';
            host.appendChild(s);
        }
    }
    // ——— Yasal Uyarı Bant (dismiss + kalıcılık) ———
    function initLegalNoticeBar() {
        const key = 'seridoviz.legal.v1';
        const dismissed = localStorage.getItem(key) === '1';
        const bar = document.getElementById('legalNoticeBar');
        if (!bar) return;
        if (dismissed) { bar.remove(); return; }
        const btn = bar.querySelector('.btn-close');
        if (btn) {
            btn.addEventListener('click', () => {
                try { localStorage.setItem(key, '1'); } catch (_) { }
                bar.style.transition = 'opacity .18s ease';
                bar.style.opacity = '0';
                setTimeout(() => bar.remove(), 180);
            });
        }
    }

    // ——— Tablo altına mini uyarı enjekte (Döviz & Index) ———
    function initLegalInlineForRates() {
        const anchors = [
            // Döviz sayfasındaki iki büyük kart bloğunun altına koy
            '.col-lg-6 .sg-card.box.p-3.h-100:last-child',
            // Index kahraman kartı altı
            '#home .sg-card.p-3.gold-border'
        ];
        anchors.forEach(sel => {
            const host = document.querySelector(sel);
            if (!host) return;
            if (host.querySelector('.legal-inline')) return;
            const p = document.createElement('p');
            p.className = 'legal-inline d-flex align-items-center gap-2';
            p.innerHTML = '<i class="bi bi-shield-exclamation"></i> ' +
                'Paylaşılan fiyatlar bilgilendirme amaçlıdır; yatırım tavsiyesi değildir.';
            host.appendChild(p);
        });
    }

    // ——— İlk ziyarette bir kere gösterilen toast (opsiyonel) ———
    function initLegalToastOnce() {
        const key = 'seridoviz.legal.toast.v1';
        if (localStorage.getItem(key) === '1') return;

        // Toast container
        let cont = document.querySelector('.toast-container');
        if (!cont) {
            cont = document.createElement('div');
            cont.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            cont.style.zIndex = '1080';
            document.body.appendChild(cont);
        }

        cont.insertAdjacentHTML('beforeend', `
  <div id="legalToast" class="toast toast-seri align-items-center" role="status" aria-live="polite" aria-atomic="true" data-bs-delay="6000">
    <div class="toast-header">
      <i class="bi bi-bell me-2"></i>
      <strong class="me-auto">Bilgilendirme</strong>
      <small>Şimdi</small>
      <button type="button" class="btn-close ms-2 mb-1" data-bs-dismiss="toast" aria-label="Kapat"></button>
    </div>
    <div class="toast-body">
      Paylaşılan fiyatlar bilgilendirme amaçlıdır; yatırım tavsiyesi değildir.
    </div>
  </div>`);

        // Gösterim tetikleyici: küçük bir gecikme veya kurlar geldiğinde
        const t = document.getElementById('legalToast');
        if (!t) return;
        const toast = new bootstrap.Toast(t);
        setTimeout(() => toast.show(), 900);

        t.addEventListener('hidden.bs.toast', () => {
            try { localStorage.setItem(key, '1'); } catch (_) { }
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        initPreferredTheme();
        markActiveNav();
        initTooltips();
        initRipple();
        wireThemeButton();
        wireToTop();
        closeNavOnClick();
        anchorScroll();
        handleScroll();
        initReveal();
        initTilt();
        initAmbient();
        initLegalNoticeBar();
        initLegalInlineForRates();
        initLegalToastOnce();
    });
    document.addEventListener('click', (e) => {
        const a = e.target.closest('a[href^="#"]');
        if (!a) return;
        // Anchor hedefi varsa, scroll sonrası aktifliği tazele
        setTimeout(() => markActiveNav(), 50);
    }, { passive: true });
    window.addEventListener('hashchange', markActiveNav, { passive: true });

    window.addEventListener('scroll', handleScroll, { passive: true });
})();
